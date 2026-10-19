namespace FoodDiary.API.Features.Workouts.Contracts;

public record GetWorkoutSetsResponse(IReadOnlyList<WorkoutSetItem> WorkoutSets);

public record WorkoutSetItem(int Id, DateOnly Date, string Exercise, int Reps, decimal WeightKg);
