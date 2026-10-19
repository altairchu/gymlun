using FoodDiary.Domain.Workouts;

namespace FoodDiary.API.Mcp.Tools.Workouts;

public static class WorkoutsToolMapper
{
    /// <returns>
    /// Days that have sets, oldest first; within a day, exercises in order of their first set, and each
    /// exercise's sets in the order they were logged
    /// </returns>
    public static IReadOnlyList<WorkoutDay> ToWorkoutDays(this IEnumerable<WorkoutSet> sets) => sets
        .OrderBy(set => set.Date)
        .ThenBy(set => set.Id)
        .GroupBy(set => set.Date)
        .Select(day => new WorkoutDay(
            day.Key,
            day.GroupBy(set => set.Exercise)
                .Select(exercise => new WorkoutExercise(
                    exercise.Key,
                    exercise.Select(set => new WorkoutSetEntry(set.Id, set.Reps, set.WeightKg)).ToList()))
                .ToList()))
        .ToList();
}
