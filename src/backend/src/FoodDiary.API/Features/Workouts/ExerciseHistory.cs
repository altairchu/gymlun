using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Workouts;

public record ExerciseSummary(string Name, DateOnly LastDone, int DaysDone);

public record ExerciseDay(string Exercise, DateOnly Date, int LastSetId);

public class ExerciseHistory(FoodDiaryContext context)
{
    public const int MaxExercises = 200;

    public async Task<IReadOnlyList<ExerciseSummary>> MostRecentlyDoneFirst(CancellationToken cancellationToken)
    {
        var exerciseDays = await context.WorkoutSets
            .AsNoTracking()
            .GroupBy(s => new { s.Exercise, s.Date })
            .Select(g => new ExerciseDay(g.Key.Exercise, g.Key.Date, g.Max(s => s.Id)))
            .ToListAsync(cancellationToken);

        return Summarize(exerciseDays);
    }

    /// <returns>
    /// At most <see cref="MaxExercises"/> exercises, the one done on the latest date first; on the same date,
    /// the one whose last set was logged later comes first
    /// </returns>
    public static IReadOnlyList<ExerciseSummary> Summarize(IEnumerable<ExerciseDay> exerciseDays) => exerciseDays
        .GroupBy(day => day.Exercise)
        .Select(days =>
        {
            var lastDay = days.MaxBy(day => (day.Date, day.LastSetId))!;
            return new { Summary = new ExerciseSummary(days.Key, lastDay.Date, days.Count()), lastDay.LastSetId };
        })
        .OrderByDescending(exercise => exercise.Summary.LastDone)
        .ThenByDescending(exercise => exercise.LastSetId)
        .Take(MaxExercises)
        .Select(exercise => exercise.Summary)
        .ToList();
}
