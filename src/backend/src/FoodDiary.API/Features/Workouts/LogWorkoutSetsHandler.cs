using FoodDiary.API.Features.Workouts.Contracts;
using FoodDiary.Domain.Workouts;
using FoodDiary.Infrastructure;

namespace FoodDiary.API.Features.Workouts;

public class LogWorkoutSetsHandler(FoodDiaryContext context, ExerciseNames exerciseNames)
{
    public const int MaxSets = 30;

    public async Task<IResult> Handle(LogWorkoutSetsRequest request, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        errors.Add("date", request.Date is null ? "Date is required." : null);
        errors.Add("exercise", WorkoutSetRules.ExerciseError(request.Exercise));
        errors.Add("sets", request.Sets is not { Count: >= 1 and <= MaxSets }
            ? $"Requested {request.Sets?.Count ?? 0} sets; pass 1 to {MaxSets}."
            : null);

        var sets = request.Sets ?? [];

        for (var index = 0; index < sets.Count; index++)
        {
            errors.Add($"sets[{index}].reps", WorkoutSetRules.RepsError(sets[index].Reps));
            errors.Add($"sets[{index}].weightKg", WorkoutSetRules.WeightKgError(sets[index].WeightKg));
        }

        if (errors.ToResult() is { } validationProblem)
        {
            return validationProblem;
        }

        var exercise = (await exerciseNames.ReuseLoggedSpellings([request.Exercise!], null, cancellationToken)).Single();

        var workoutSets = sets
            .Select(set => new WorkoutSet
            {
                Date = request.Date!.Value,
                Exercise = exercise,
                Reps = set.Reps!.Value,
                WeightKg = WorkoutSetRules.RoundWeightKg(set.WeightKg!.Value)
            })
            .ToList();

        await context.AddInOrder(workoutSets, cancellationToken);

        return Results.Ok(new LogWorkoutSetsResponse(workoutSets.Select(s => s.Id).ToList()));
    }
}
