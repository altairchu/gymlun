using System.ComponentModel;
using FoodDiary.API.Features.Workouts;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Workouts;

[McpServerToolType]
public class WorkoutsTool(FoodDiaryContext context, ExerciseHistory exerciseHistory)
{
    private const string GetWorkoutsToolDescription =
        """
        The person's strength training over a date range: the sets done each day, grouped by exercise. Days run oldest first, and only days with sets are listed.

        Within a day, exercises run in the order they were started, and each exercise's sets in the order they were done. Each set has its `id`, `reps` and `weightKg`, the added weight in kilograms; `weightKg` 0 means bodyweight, with no added weight.

        `from` and `to` are inclusive calendar dates with no time or timezone. The range spans at most 366 days; split longer periods into several calls.
        """;

    private const string ListExercisesToolDescription =
        """
        The exercises the person has logged, most recently done first, at most 200: each one's `name`, the date it was last done (`lastDone`) and on how many days it was done (`daysDone`).

        Call it before `log_workout` and reuse these names exactly, so an exercise done again keeps one name, e.g. "Bench Press" rather than a new "Barbell bench".
        """;

    private const string DateParameterDescription = "Inclusive calendar date, `yyyy-MM-dd`.";

    [McpServerTool(Name = "get_workouts", ReadOnly = true, OpenWorld = false)]
    [Description(GetWorkoutsToolDescription)]
    public async Task<GetWorkoutsResponse> GetWorkouts(
        [Description(DateParameterDescription)] DateOnly from,
        [Description(DateParameterDescription)] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (WorkoutSetRules.RangeError(from, to) is { } rangeError)
        {
            throw new McpException(rangeError);
        }

        var sets = await context.WorkoutSets
            .AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= to)
            .ToListAsync(cancellationToken);

        return new GetWorkoutsResponse(from, to, sets.ToWorkoutDays());
    }

    [McpServerTool(Name = "list_exercises", ReadOnly = true, OpenWorld = false)]
    [Description(ListExercisesToolDescription)]
    public async Task<ListExercisesResponse> ListExercises(CancellationToken cancellationToken)
    {
        var exercises = await exerciseHistory.MostRecentlyDoneFirst(cancellationToken);

        return new ListExercisesResponse(exercises
            .Select(exercise => new ListedExercise(exercise.Name, exercise.LastDone, exercise.DaysDone))
            .ToList());
    }
}
