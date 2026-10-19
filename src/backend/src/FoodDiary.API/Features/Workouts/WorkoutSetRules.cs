using FoodDiary.Domain.Workouts;

namespace FoodDiary.API.Features.Workouts;

public static class WorkoutSetRules
{
    public const int MaxRangeDays = 366;

    public static string? RangeError(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            return $"`from` {from:yyyy-MM-dd} is after `to` {to:yyyy-MM-dd}.";
        }

        var rangeDays = to.DayNumber - from.DayNumber + 1;

        return rangeDays > MaxRangeDays
            ? $"Requested {rangeDays} days; the maximum is {MaxRangeDays}. Split the range."
            : null;
    }

    public static string? ExerciseError(string? exercise)
    {
        if (string.IsNullOrWhiteSpace(exercise))
        {
            return "Exercise is required.";
        }

        var length = exercise.Trim().Length;

        return length > WorkoutSet.MaxExerciseLength
            ? $"Exercise is {length} characters long; it must be at most {WorkoutSet.MaxExerciseLength}."
            : null;
    }

    public static string? RepsError(int? reps) => reps switch
    {
        null => "Reps are required.",
        < WorkoutSet.MinReps or > WorkoutSet.MaxReps =>
            $"Requested {reps} reps; they must be from {WorkoutSet.MinReps} to {WorkoutSet.MaxReps}.",
        _ => null
    };

    public static string? WeightKgError(decimal? weightKg) => weightKg switch
    {
        null => "Weight is required; pass 0 for bodyweight.",
        < WorkoutSet.MinWeightKg or > WorkoutSet.MaxWeightKg =>
            $"Requested weight {weightKg}; it must be from {WorkoutSet.MinWeightKg} to {WorkoutSet.MaxWeightKg} kg.",
        _ => null
    };

    public static decimal RoundWeightKg(decimal weightKg) => Math.Round(weightKg, 2, MidpointRounding.AwayFromZero);
}
