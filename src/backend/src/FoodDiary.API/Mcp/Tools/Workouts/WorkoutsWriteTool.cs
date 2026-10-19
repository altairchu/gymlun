using System.ComponentModel;
using FoodDiary.API.Features.Workouts;
using FoodDiary.Domain.Workouts;
using FoodDiary.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Workouts;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class WorkoutsWriteTool(FoodDiaryContext context, ExerciseNames exerciseNames)
{
    internal const int MaxDeleteWorkoutSetsIds = 50;

    private const string LogWorkoutToolDescription =
        """
        Logs a strength training session on one date: each exercise with its sets, in the order they were done. Returns the sets it logged, grouped by exercise, with their `id`. Log a whole session in one call, so the person approves it once: `exercises` holds 1 to 20 exercises and up to 100 sets in total.

        Call `list_exercises` first and reuse the person's exercise names. A name that matches a logged one, ignoring case, is stored with the logged spelling.

        Each set has `reps` and `weightKg`, the added weight in kilograms: pass 0 for bodyweight exercises such as pull-ups. Convert pounds to kilograms first (1 lb = 0.4536 kg).

        `date` is a calendar date with no time or timezone: use the person's local date. Every exercise and set is checked before any is logged: when one is out of range, nothing is logged.
        """;

    private const string UpdateWorkoutSetToolDescription =
        """
        Changes one set by its `id`, as returned by `get_workouts` or `log_workout`, and returns it as it is now. Pass only what changes: everything else stays as it was.

        `weightKg` is the added weight in kilograms, 0 for bodyweight; convert pounds first (1 lb = 0.4536 kg). `date` moves the set to another day; `exercise` renames it, reusing a logged spelling that matches ignoring case.
        """;

    private const string DeleteWorkoutSetsToolDescription =
        """
        Deletes sets by their `id`, as returned by `get_workouts` or `log_workout`: pass every set to delete in `ids`, 1 to 50 of them, in one call. Use it to undo sets logged by mistake; to fix a wrong value, use `update_workout_set` instead.

        When any of `ids` is not found, nothing is deleted.
        """;

    [McpServerTool(Name = "log_workout", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(LogWorkoutToolDescription)]
    public async Task<WorkoutDay> LogWorkout(
        [Description("Calendar date, `yyyy-MM-dd`.")] DateOnly date,
        [Description("Exercises done, 1 to 20, in the order they were done, each with its sets.")] LogWorkoutExercise[] exercises,
        CancellationToken cancellationToken)
    {
        LogWorkoutArgument.Validate(exercises);

        var names = await exerciseNames.ReuseLoggedSpellings(
            exercises.Select(exercise => exercise.Exercise).ToList(),
            null,
            cancellationToken);

        var sets = exercises
            .SelectMany((exercise, index) => exercise.Sets.Select(set => new WorkoutSet
            {
                Date = date,
                Exercise = names[index],
                Reps = set.Reps,
                WeightKg = WorkoutSetRules.RoundWeightKg(set.WeightKg)
            }))
            .ToList();

        await context.AddInOrder(sets, cancellationToken);

        return sets.ToWorkoutDays().Single();
    }

    [McpServerTool(Name = "update_workout_set", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(UpdateWorkoutSetToolDescription)]
    public async Task<UpdateWorkoutSetResponse> UpdateWorkoutSet(
        [Description("Id of the set to change.")] int id,
        [Description("New calendar date, `yyyy-MM-dd`.")] DateOnly? date = null,
        [Description("New exercise name.")] string? exercise = null,
        [Description("New repetitions, from 1 to 1000.")] int? reps = null,
        [Description("New added weight in kilograms, from 0 to 1000; 0 for bodyweight.")] decimal? weightKg = null,
        CancellationToken cancellationToken = default)
    {
        var error = (exercise is null ? null : WorkoutSetRules.ExerciseError(exercise))
                    ?? (reps is null ? null : WorkoutSetRules.RepsError(reps))
                    ?? (weightKg is null ? null : WorkoutSetRules.WeightKgError(weightKg));

        if (error is not null)
        {
            throw new McpException(error);
        }

        var set = await context.WorkoutSets.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                  ?? throw new McpException($"Workout set {id} not found. Find it with `get_workouts`.");

        if (exercise is not null)
        {
            set.Exercise = (await exerciseNames.ReuseLoggedSpellings([exercise], id, cancellationToken)).Single();
        }

        set.Date = date ?? set.Date;
        set.Reps = reps ?? set.Reps;
        set.WeightKg = weightKg is { } newWeightKg ? WorkoutSetRules.RoundWeightKg(newWeightKg) : set.WeightKg;
        await context.SaveChangesAsync(cancellationToken);

        return new UpdateWorkoutSetResponse(set.Id, set.Date, set.Exercise, set.Reps, set.WeightKg);
    }

    [McpServerTool(Name = "delete_workout_sets", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(DeleteWorkoutSetsToolDescription)]
    public async Task<DeleteWorkoutSetsResponse> DeleteWorkoutSets(
        [Description("Ids of the sets to delete, 1 to 50.")] int[] ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = IdsArgument.Distinct(ids, MaxDeleteWorkoutSetsIds);

        var foundIds = await context.WorkoutSets
            .Where(s => distinctIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var missingIds = distinctIds.Except(foundIds).ToList();

        if (missingIds.Count > 0)
        {
            throw new McpException(
                $"Workout sets not found: {IdsArgument.Join(missingIds)}. Nothing was deleted; find them with `get_workouts`.");
        }

        await context.WorkoutSets
            .Where(s => distinctIds.Contains(s.Id))
            .ExecuteDeleteAsync(cancellationToken);

        return new DeleteWorkoutSetsResponse(distinctIds);
    }
}
