using FoodDiary.API.Features.Workouts.Contracts;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Workouts;

public class GetWorkoutSetsHandler(FoodDiaryContext context)
{
    public async Task<IResult> Handle(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from is null || to is null)
        {
            var errors = new ValidationErrors();
            errors.Add("from", from is null ? "`from` is required." : null);
            errors.Add("to", to is null ? "`to` is required." : null);
            return errors.ToResult()!;
        }

        if (WorkoutSetRules.RangeError(from.Value, to.Value) is { } rangeError)
        {
            var errors = new ValidationErrors();
            errors.Add("to", rangeError);
            return errors.ToResult()!;
        }

        var workoutSets = await context.WorkoutSets
            .AsNoTracking()
            .Where(s => s.Date >= from.Value && s.Date <= to.Value)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .Select(s => s.ToWorkoutSetItem())
            .ToListAsync(cancellationToken);

        return Results.Ok(new GetWorkoutSetsResponse(workoutSets));
    }
}
