using FoodDiary.API.Features.Workouts;
using ModelContextProtocol;

namespace FoodDiary.API.Mcp.Tools.Workouts;

public static class LogWorkoutArgument
{
    public const int MaxExercises = 20;
    public const int MaxSets = 100;

    public static void Validate(LogWorkoutExercise[] exercises)
    {
        if (exercises.Length is < 1 or > MaxExercises)
        {
            throw new McpException($"Requested {exercises.Length} exercises; pass 1 to {MaxExercises}.");
        }

        var setCount = exercises.Sum(exercise => exercise.Sets.Length);

        if (setCount > MaxSets)
        {
            throw new McpException($"Requested {setCount} sets in total; pass at most {MaxSets}. Nothing was logged.");
        }

        for (var exerciseIndex = 0; exerciseIndex < exercises.Length; exerciseIndex++)
        {
            var exercise = exercises[exerciseIndex];

            if (WorkoutSetRules.ExerciseError(exercise.Exercise) is { } exerciseError)
            {
                throw new McpException($"`exercises[{exerciseIndex}]`: {exerciseError} Nothing was logged.");
            }

            if (exercise.Sets.Length == 0)
            {
                throw new McpException(
                    $"`exercises[{exerciseIndex}]` ({exercise.Exercise.Trim()}): pass at least 1 set. Nothing was logged.");
            }

            for (var setIndex = 0; setIndex < exercise.Sets.Length; setIndex++)
            {
                var set = exercise.Sets[setIndex];

                if ((WorkoutSetRules.RepsError(set.Reps) ?? WorkoutSetRules.WeightKgError(set.WeightKg)) is { } setError)
                {
                    throw new McpException(
                        $"`exercises[{exerciseIndex}].sets[{setIndex}]` ({exercise.Exercise.Trim()}): {setError} Nothing was logged.");
                }
            }
        }
    }
}
