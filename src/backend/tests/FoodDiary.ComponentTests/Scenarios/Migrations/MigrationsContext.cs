using FoodDiary.ComponentTests.Infrastructure.DataAccess;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace FoodDiary.ComponentTests.Scenarios.Migrations;

[UsedImplicitly]
public class MigrationsContext(DatabaseFixture database)
{
    private string _connectionString = null!;

    public async Task Given_database_migrated_to(string migration)
    {
        var databaseName = $"migrations-{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"""CREATE DATABASE "{databaseName}";""", connection);
            await command.ExecuteNonQueryAsync();
        }

        _connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = databaseName }
            .ConnectionString;

        await MigrateTo(migration);
    }

    public Task Given_category(int id, string name) => Execute(
        """INSERT INTO "Categories" ("Id", "Name") VALUES (@id, @name)""",
        ("id", id),
        ("name", name));

    public Task Given_product_in_category(int id, string name, int categoryId) => Execute(
        """INSERT INTO "Products" ("Id", "Name", "CaloriesCost", "CategoryId") VALUES (@id, @name, 100, @categoryId)""",
        ("id", id),
        ("name", name),
        ("categoryId", categoryId));

    public Task Given_product_in_categories(int id, string name, params int[] categoryIds) => Execute(
        """
        INSERT INTO "Products" ("Id", "Name", "CaloriesCost") VALUES (@id, @name, 100);
        INSERT INTO "ProductCategories" ("ProductId", "CategoryId") SELECT @id, unnest(@categoryIds);
        """,
        ("id", id),
        ("name", name),
        ("categoryIds", categoryIds));

    public Task Given_note(int id, int productId, MealType mealType) => Execute(
        """
        INSERT INTO "Notes" ("Id", "Date", "MealType", "ProductId", "ProductQuantity", "DisplayOrder")
        VALUES (@id, DATE '2026-09-01', @mealType, @productId, 100, 0)
        """,
        ("id", id),
        ("mealType", (int)mealType),
        ("productId", productId));

    public Task When_database_is_migrated_to(string migration) => MigrateTo(migration);

    public Task Given_other_legacy_data(int productId) => Execute(
        """
        INSERT INTO "ProductPhotos" ("ProductId", "Content") VALUES (@productId, DECODE('0102', 'hex'));
        INSERT INTO "WeightLogs" ("Date", "Weight") VALUES (DATE '2026-09-01', 72);
        INSERT INTO "WorkoutSets" ("Id", "Date", "Exercise", "Reps", "WeightKg")
        VALUES (227, DATE '2026-09-01', 'Squat', 8, 30);
        """,
        ("productId", productId));

    public async Task Then_all_legacy_records_are_owned_by_zero(int productId, int noteId, int categoryId)
    {
        var owners = await Query<long>(
            """
            SELECT "OwnerId" FROM "Categories" WHERE "Id" = @categoryId
            UNION ALL SELECT "OwnerId" FROM "Products" WHERE "Id" = @productId
            UNION ALL SELECT "OwnerId" FROM "ProductCategories" WHERE "ProductId" = @productId AND "CategoryId" = @categoryId
            UNION ALL SELECT "OwnerId" FROM "Notes" WHERE "Id" = @noteId AND "ProductId" = @productId
            UNION ALL SELECT "OwnerId" FROM "ProductPhotos" WHERE "ProductId" = @productId AND "Content" = DECODE('0102', 'hex')
            UNION ALL SELECT "OwnerId" FROM "WeightLogs" WHERE "Date" = DATE '2026-09-01' AND "Weight" = 72
            UNION ALL SELECT "OwnerId" FROM "WorkoutSets" WHERE "Id" = 227 AND "Exercise" = 'Squat'
            """,
            ("categoryId", categoryId),
            ("productId", productId),
            ("noteId", noteId));
        owners.Should().Equal(Enumerable.Repeat(0L, 7));
    }

    public async Task Then_product_is_in_categories(int productId, params int[] categoryIds)
    {
        var linkedCategoryIds = await Query<int>(
            """SELECT "CategoryId" FROM "ProductCategories" WHERE "ProductId" = @productId""",
            ("productId", productId));

        linkedCategoryIds.Should().BeEquivalentTo(categoryIds);
    }

    public async Task Then_product_is_counted_in_grams(int productId)
    {
        var quantityUnits = await Query<int>(
            """SELECT "QuantityUnit" FROM "Products" WHERE "Id" = @productId""",
            ("productId", productId));

        quantityUnits.Should().Equal((int)QuantityUnit.Grams);
    }

    public async Task Then_note_is_eaten_at(int noteId, string time)
    {
        var times = await Query<TimeOnly>(
            """SELECT "Time" FROM "Notes" WHERE "Id" = @noteId""",
            ("noteId", noteId));

        times.Should().Equal(TimeOnly.Parse(time));
    }

    public async Task Then_product_is_in_single_category(int productId, string categoryName)
    {
        var categoryNames = await Query<string>(
            """
            SELECT c."Name" FROM "Products" p JOIN "Categories" c ON c."Id" = p."CategoryId"
            WHERE p."Id" = @productId
            """,
            ("productId", productId));

        categoryNames.Should().Equal(categoryName);
    }

    private async Task MigrateTo(string migration)
    {
        var options = new DbContextOptionsBuilder<FoodDiaryContext>().UseNpgsql(_connectionString).Options;
        await using var context = new FoodDiaryContext(options);
        await context.GetService<IMigrator>().MigrateAsync(migration);
    }

    private async Task Execute(string sql, params (string Name, object Value)[] parameters)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var command = dataSource.CreateCommand(sql);
        AddParameters(command, parameters);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<T>> Query<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var command = dataSource.CreateCommand(sql);
        AddParameters(command, parameters);

        var values = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            values.Add(reader.GetFieldValue<T>(0));
        }

        return values;
    }

    private static void AddParameters(NpgsqlCommand command, (string Name, object Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
    }
}
