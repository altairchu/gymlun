using System.ComponentModel;
using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.Workouts;

[PublicAPI]
public record LogWorkoutExercise
{
    [Description("Name of the exercise, e.g. Bench Press; reuse a name from `list_exercises` when it is the same exercise.")]
    public required string Exercise { get; init; }

    [Description("Sets of the exercise in the order they were done.")]
    public required LogWorkoutSet[] Sets { get; init; }
}

[PublicAPI]
public record LogWorkoutSet
{
    [Description("Repetitions, from 1 to 1000.")]
    public required int Reps { get; init; }

    [Description("Added weight in kilograms, from 0 to 1000, e.g. 62.5; 0 for bodyweight.")]
    public required decimal WeightKg { get; init; }
}
