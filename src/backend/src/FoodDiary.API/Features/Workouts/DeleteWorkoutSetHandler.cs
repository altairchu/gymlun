using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Workouts;

public class DeleteWorkoutSetHandler(FoodDiaryContext context)
{
    public async Task<IResult> Handle(int id, CancellationToken cancellationToken)
    {
        var deletedCount = await context.WorkoutSets
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount == 0 ? Results.NotFound() : Results.NoContent();
    }
}
