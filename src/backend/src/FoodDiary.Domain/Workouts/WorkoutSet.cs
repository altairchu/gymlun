namespace FoodDiary.Domain.Workouts;

/// <summary>
/// One set of an exercise; the sets of a day run in order of <see cref="Id"/>, the order they were logged
/// </summary>
public class WorkoutSet
{
    public const int MaxExerciseLength = 100;
    public const int MinReps = 1;
    public const int MaxReps = 1000;
    public const decimal MinWeightKg = 0;
    public const decimal MaxWeightKg = 1000;

    public int Id { get; init; }
    public required DateOnly Date { get; set; }
    public required string Exercise { get; set; }
    public required int Reps { get; set; }

    /// <summary>
    /// Added weight; 0 for bodyweight
    /// </summary>
    public required decimal WeightKg { get; set; }
}
