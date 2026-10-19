using FoodDiary.API.Features.Workouts.Contracts;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Workouts;

public class UpdateWorkoutSetHandler(FoodDiaryContext context, ExerciseNames exerciseNames)
{
    public async Task<IResult> Handle(int id, UpdateWorkoutSetRequest request, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        errors.Add("date", request.Date is null ? "Date is required." : null);
        errors.Add("exercise", WorkoutSetRules.ExerciseError(request.Exercise));
        errors.Add("reps", WorkoutSetRules.RepsError(request.Reps));
        errors.Add("weightKg", WorkoutSetRules.WeightKgError(request.WeightKg));

        if (errors.ToResult() is { } validationProblem)
        {
            return validationProblem;
        }

        var workoutSet = await context.WorkoutSets.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (workoutSet is null)
        {
            return Results.NotFound();
        }

        workoutSet.Date = request.Date!.Value;
        workoutSet.Exercise = (await exerciseNames.ReuseLoggedSpellings([request.Exercise!], id, cancellationToken)).Single();
        workoutSet.Reps = request.Reps!.Value;
        workoutSet.WeightKg = WorkoutSetRules.RoundWeightKg(request.WeightKg!.Value);
        await context.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
