using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FoodDiary.Domain.Abstractions;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.WeightTracking;
using FoodDiary.Domain.Workouts;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FoodDiary.Infrastructure;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public partial class FoodDiaryContext(DbContextOptions options, IFoodDiaryOwner? diaryOwner = null)
    : DbContext(options), IUnitOfWork, IDataProtectionKeyContext
{
    public long OwnerId => diaryOwner?.Id ?? IDiaryOwned.UnassignedOwnerId;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; init; }
    public DbSet<Note> Notes { get; init; }
    public DbSet<Product> Products { get; init; }
    public DbSet<Category> Categories { get; init; }
    public DbSet<WeightLog> WeightLogs { get; init; }
    public DbSet<ProductPhoto> ProductPhotos { get; init; }
    public DbSet<WorkoutSet> WorkoutSets { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var configureOwner = typeof(FoodDiaryContext).GetMethod(nameof(ConfigureDiaryOwner),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var entity in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            if (typeof(IDiaryOwned).IsAssignableFrom(entity.ClrType))
                configureOwner.MakeGenericMethod(entity.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    private void ConfigureDiaryOwner<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IDiaryOwned
    {
        var entity = modelBuilder.Entity<TEntity>();
        var owner = entity.Property(record => record.OwnerId)
            .HasSentinel(IDiaryOwned.UnassignedOwnerId)
            .IsConcurrencyToken()
            .ValueGeneratedOnAdd()
            .HasValueGenerator<DiaryOwnerValueGenerator>();
        owner.Metadata.SetValueGenerationStrategy(NpgsqlValueGenerationStrategy.None);

        entity.HasQueryFilter(record => OwnerId >= 0 && record.OwnerId == OwnerId);
        var tableName = entity.Metadata.GetTableName();
        entity.ToTable(table => table.HasCheckConstraint($"CK_{tableName}_OwnerId", "\"OwnerId\" >= 0"));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateDiaryOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateDiaryOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateDiaryOwnership()
    {
        var ownerId = OwnerId;
        foreach (var entry in ChangeTracker.Entries<IDiaryOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var owner = entry.Property(record => record.OwnerId);
            if (ownerId < 0 || owner.CurrentValue != ownerId ||
                (entry.State != EntityState.Added && owner.OriginalValue != ownerId))
            {
                throw new InvalidOperationException("A diary write requires matching ownership.");
            }

            if (entry.State == EntityState.Modified)
                owner.IsModified = false;
        }
    }

    private sealed class DiaryOwnerValueGenerator : ValueGenerator<long>
    {
        public override bool GeneratesTemporaryValues => false;

        public override long Next(EntityEntry entry)
        {
            var ownerId = ((FoodDiaryContext)entry.Context).OwnerId;
            return ownerId >= 0 ? ownerId : throw new InvalidOperationException("A diary write requires an owner.");
        }
    }
}
