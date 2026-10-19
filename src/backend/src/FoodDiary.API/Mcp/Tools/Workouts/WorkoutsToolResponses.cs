using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.Workouts;

[PublicAPI]
public record GetWorkoutsResponse(DateOnly From, DateOnly To, IReadOnlyList<WorkoutDay> Days);

[PublicAPI]
public record WorkoutDay(DateOnly Date, IReadOnlyList<WorkoutExercise> Exercises);

[PublicAPI]
public record WorkoutExercise(string Exercise, IReadOnlyList<WorkoutSetEntry> Sets);

[PublicAPI]
public record WorkoutSetEntry(int Id, int Reps, decimal WeightKg);

[PublicAPI]
public record ListExercisesResponse(IReadOnlyList<ListedExercise> Exercises);

[PublicAPI]
public record ListedExercise(string Name, DateOnly LastDone, int DaysDone);

[PublicAPI]
public record UpdateWorkoutSetResponse(int Id, DateOnly Date, string Exercise, int Reps, decimal WeightKg);

[PublicAPI]
public record DeleteWorkoutSetsResponse(IReadOnlyList<int> DeletedIds);
