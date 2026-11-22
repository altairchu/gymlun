using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Abstractions;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.WeightTracking;
using FoodDiary.Domain.Workouts;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Repositories.v2;
using FoodDiary.Migrator;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.ComponentTests.Scenarios.Ownership;

[Collection(nameof(InfrastructureCollection))]
public class DiaryOwnershipTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const long OwnerA = 4101;
    private const long OwnerB = 4102;
    private static readonly DateOnly Day = new(2026, 1, 6);

    public Task InitializeAsync() => infrastructure.Database.Clear();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Authenticated_owners_can_use_the_same_names_and_dates_independently()
    {
        int firstProductId;
        int firstNoteId;
        int firstWorkoutId;
        await using (var first = CreateContext(OwnerA))
        {
            var product = ProductGraph();
            var workout = new WorkoutSet { Date = Day, Exercise = "Squat", Reps = 10, WeightKg = 60 };
            first.Products.Add(product);
            first.WeightLogs.Add(new WeightLog { Date = Day, Weight = 65 });
            first.WorkoutSets.Add(workout);
            first.GoalSettings.Add(new GoalSettings { TargetKcal = 2000 });
            await first.SaveChangesAsync();
            firstProductId = product.Id;
            firstNoteId = product.Notes.Single().Id;
            firstWorkoutId = workout.Id;
            Assert.Equal(OwnerA, product.Photo!.OwnerId);
            Assert.Equal(OwnerA, (await first.GoalSettings.SingleAsync()).OwnerId);
        }

        await using (var second = CreateContext(OwnerB))
        {
            Assert.Empty(await second.Notes.ToArrayAsync());
            Assert.Empty(await second.Products.ToArrayAsync());
            Assert.Empty(await second.ProductPhotos.ToArrayAsync());
            Assert.Empty(await second.Categories.ToArrayAsync());
            Assert.Empty(await second.WorkoutSets.ToArrayAsync());
            Assert.Empty(await second.GoalSettings.ToArrayAsync());

            var product = ProductGraph();
            second.Products.Add(product);
            second.WeightLogs.Add(new WeightLog { Date = Day, Weight = 85 });
            await second.SaveChangesAsync();

            var visibleProduct = await second.Products.Include(item => item.Categories).SingleAsync();
            Assert.Equal(OwnerB, visibleProduct.OwnerId);
            Assert.Equal(OwnerB, visibleProduct.Categories.Single().OwnerId);
            Assert.Null(await second.Products.FirstOrDefaultAsync(item => item.Id == firstProductId));
            Assert.Null(await new NotesRepository(second).FindById(firstNoteId, CancellationToken.None));
            Assert.Equal(0, await second.WorkoutSets.Where(set => set.Id == firstWorkoutId).ExecuteDeleteAsync());
            Assert.Equal(1, await second.WeightLogs.Where(log => log.Date == Day)
                .ExecuteUpdateAsync(setters => setters.SetProperty(log => log.Weight, 86)));
        }

        await using var check = CreateContext(OwnerA);
        Assert.Equal(65, (await check.WeightLogs.SingleAsync()).Weight);
        Assert.Equal(firstWorkoutId, (await check.WorkoutSets.SingleAsync()).Id);
        Assert.Equal(firstProductId, (await check.Products.SingleAsync()).Id);
    }

    [Fact]
    public async Task Ownership_constraints_reject_another_owners_relationship_and_detached_update()
    {
        int productId;
        await using (var first = CreateContext(OwnerA))
        {
            var product = ProductGraph();
            first.Products.Add(product);
            await first.SaveChangesAsync();
            productId = product.Id;
        }

        await using (var second = CreateContext(OwnerB))
        {
            second.Notes.Add(new Note { ProductId = productId, Date = Day, ProductQuantity = 100 });
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
            Assert.IsType<PostgresException>(failure.InnerException);
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ((PostgresException)failure.InnerException!).SqlState);
        }

        await using (var second = CreateContext(OwnerB))
        {
            var detached = ProductGraph();
            detached.Id = productId;
            detached.OwnerId = OwnerB;
            detached.Notes.Clear();
            detached.Categories.Clear();
            detached.Photo = null;
            second.Update(detached);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }

        await using var unauthenticated = CreateContext(null);
        Assert.Empty(await unauthenticated.Products.ToArrayAsync());
        Assert.Throws<InvalidOperationException>(() => unauthenticated.Products.Add(ProductGraph()));
    }

    [Fact]
    public async Task Claim_preserves_legacy_relationships_and_refuses_merging_with_an_existing_diary()
    {
        int legacyProductId;
        int legacyNoteId;
        await using (var legacy = CreateContext(0))
        {
            var product = ProductGraph();
            legacy.Products.Add(product);
            legacy.WeightLogs.Add(new WeightLog { Date = Day, Weight = 72 });
            await legacy.SaveChangesAsync();
            legacyProductId = product.Id;
            legacyNoteId = product.Notes.Single().Id;
        }

        await using (var existing = CreateContext(OwnerB))
        {
            existing.WeightLogs.Add(new WeightLog { Date = Day, Weight = 94 });
            await existing.SaveChangesAsync();
        }

        await using var administrator = CreateContext(null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => LegacyDiaryOwnership.ClaimAsync(administrator, OwnerB));
        Assert.Equal(6, await LegacyDiaryOwnership.ClaimAsync(administrator, OwnerA));
        Assert.Equal(0, await LegacyDiaryOwnership.ClaimAsync(administrator, OwnerA));

        await using var claimed = CreateContext(OwnerA);
        var moved = await claimed.Products.Include(item => item.Categories).Include(item => item.Photo)
            .Include(item => item.Notes).SingleAsync();
        Assert.Equal(legacyProductId, moved.Id);
        Assert.Equal(legacyNoteId, moved.Notes.Single().Id);
        Assert.Equal(OwnerA, moved.Categories.Single().OwnerId);
        Assert.Equal(OwnerA, moved.Photo!.OwnerId);
        Assert.Equal(72, (await claimed.WeightLogs.SingleAsync()).Weight);

        await using var quarantine = CreateContext(0);
        Assert.Empty(await quarantine.Products.ToArrayAsync());
        await using var untouched = CreateContext(OwnerB);
        Assert.Equal(94, (await untouched.WeightLogs.SingleAsync()).Weight);
    }

    private FoodDiaryContext CreateContext(long? owner)
    {
        var options = new DbContextOptionsBuilder<FoodDiaryContext>()
            .UseNpgsql(infrastructure.Database.ConnectionString)
            .Options;
        return new FoodDiaryContext(options, owner is { } id ? new TestOwner(id) : null);
    }

    private static Product ProductGraph() => new()
    {
        Name = "Rice",
        CaloriesCost = 135,
        Protein = null,
        Fats = null,
        Carbs = null,
        Sugar = null,
        Salt = null,
        Categories = [new Category { Name = "Lunch" }],
        Notes = [new Note { Date = Day, MealType = MealType.Lunch, ProductQuantity = 100 }],
        Photo = new ProductPhoto { Content = [1, 2, 3] }
    };

    private sealed record TestOwner(long Id) : IFoodDiaryOwner;
}
