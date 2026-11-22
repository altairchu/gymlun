using System.Runtime.CompilerServices;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Migrator;

public static class LegacyDiaryOwnership
{
    private static readonly string[] Tables =
    [
        "Categories", "Products", "ProductCategories", "Notes", "ProductPhotos", "WeightLogs", "WorkoutSets"
    ];

    public static async Task<long> ClaimAsync(FoodDiaryContext context, long ownerId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ownerId);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            LOCK TABLE "Categories", "Products", "ProductCategories", "Notes", "ProductPhotos", "WeightLogs", "WorkoutSets"
            IN ACCESS EXCLUSIVE MODE
            """,
            cancellationToken);

        var legacyRows = await CountRowsAsync(context, 0, cancellationToken);
        if (legacyRows == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        if (await CountRowsAsync(context, ownerId, cancellationToken) != 0)
            throw new InvalidOperationException("The selected diary already has data. The legacy diary was not transferred.");

        await context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED", cancellationToken);
        long rowsMoved = 0;
        foreach (var table in Tables)
        {
            var move = FormattableStringFactory.Create(
                $"UPDATE \"{table}\" SET \"OwnerId\" = {{0}} WHERE \"OwnerId\" = 0", ownerId);
            rowsMoved += await context.Database.ExecuteSqlInterpolatedAsync(move, cancellationToken);
        }
        await context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE", cancellationToken);

        if (rowsMoved != legacyRows || await CountRowsAsync(context, 0, cancellationToken) != 0 ||
            await CountRowsAsync(context, ownerId, cancellationToken) != legacyRows)
        {
            throw new InvalidOperationException("The diary transfer did not reconcile. The legacy diary was not transferred.");
        }

        await transaction.CommitAsync(cancellationToken);
        return rowsMoved;
    }

    private static async Task<long> CountRowsAsync(FoodDiaryContext context, long ownerId, CancellationToken cancellationToken)
    {
        long rows = 0;
        foreach (var table in Tables)
        {
            var count = FormattableStringFactory.Create(
                $"SELECT COUNT(*) AS \"Value\" FROM \"{table}\" WHERE \"OwnerId\" = {{0}}", ownerId);
            rows += await context.Database.SqlQuery<long>(count).SingleAsync(cancellationToken);
        }
        return rows;
    }
}
