namespace FoodDiary.API.Features.Workouts.Contracts;

public record LogWorkoutSetsRequest(DateOnly? Date, string? Exercise, IReadOnlyList<LogWorkoutSetsRequestSet>? Sets);

public record LogWorkoutSetsRequestSet(int? Reps, decimal? WeightKg);

public record LogWorkoutSetsResponse(IReadOnlyList<int> Ids);
