using FoodDiary.Domain.Workouts;
using FoodDiary.Infrastructure;

namespace FoodDiary.API.Features.Workouts;

public static class WorkoutSetsLog
{
    /// <summary>
    /// Saves the sets all or none, each one before the next, so their ids follow the order they were done in
    /// </summary>
    public static async Task AddInOrder(
        this FoodDiaryContext context,
        IEnumerable<WorkoutSet> sets,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var set in sets)
        {
            context.WorkoutSets.Add(set);
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
