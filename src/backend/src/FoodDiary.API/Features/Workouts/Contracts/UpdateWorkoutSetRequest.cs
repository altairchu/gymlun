namespace FoodDiary.API.Features.Workouts.Contracts;

public record UpdateWorkoutSetRequest(DateOnly? Date, string? Exercise, int? Reps, decimal? WeightKg);
