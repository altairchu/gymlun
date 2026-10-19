using System.Globalization;
using System.Text.Json;
using FoodDiary.API.Mcp.Tools.Workouts;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Workouts;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ComponentTests.Scenarios.Mcp;

public partial class McpApiContext
{
    public async Task Given_workout_sets(params WorkoutSet[] workoutSets)
    {
        foreach (var workoutSet in workoutSets)
        {
            await Factory.SeedDataAsync([workoutSet]);
        }
    }

    public Task When_mcp_client_calls_get_workouts(string from, string to)
    {
        return When_mcp_client_calls_listed_tool("get_workouts", new Dictionary<string, object?>
        {
            ["from"] = from,
            ["to"] = to
        });
    }

    public Task When_mcp_client_calls_list_exercises()
    {
        return When_mcp_client_calls_listed_tool("list_exercises", new Dictionary<string, object?>());
    }

    public Task When_mcp_client_calls_log_workout(string date, params LogWorkoutExercise[] exercises)
    {
        return When_mcp_client_calls_listed_tool("log_workout", new Dictionary<string, object?>
        {
            ["date"] = date,
            ["exercises"] = exercises
                .Select(exercise => new Dictionary<string, object?>
                {
                    ["exercise"] = exercise.Exercise,
                    ["sets"] = exercise.Sets
                        .Select(set => new Dictionary<string, object?> { ["reps"] = set.Reps, ["weightKg"] = set.WeightKg })
                        .ToArray()
                })
                .ToArray()
        });
    }

    public Task When_mcp_client_calls_update_workout_set(WorkoutSet workoutSet, int reps, decimal weightKg)
    {
        return When_mcp_client_calls_listed_tool("update_workout_set", new Dictionary<string, object?>
        {
            ["id"] = workoutSet.Id,
            ["reps"] = reps,
            ["weightKg"] = weightKg
        });
    }

    public Task When_mcp_client_calls_delete_workout_sets(params WorkoutSet[] workoutSets)
    {
        return When_mcp_client_calls_delete_workout_sets_by_ids(workoutSets.Select(s => s.Id).ToArray());
    }

    public Task When_mcp_client_calls_delete_workout_sets_by_ids(params int[] ids)
    {
        return When_mcp_client_calls_listed_tool("delete_workout_sets", new Dictionary<string, object?>
        {
            ["ids"] = ids
        });
    }

    /// <param name="days">Each day as <c>yyyy-MM-dd: Exercise reps x kg, reps x kg; Exercise reps x kg</c></param>
    public Task Then_workout_days_are(params string[] days)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("days").EnumerateArray()
            .Select(day => $"{day.GetProperty("date").GetString()}: {DescribeExercises(day)}")
            .Should().Equal(days);

        return Task.CompletedTask;
    }

    public Task Then_exercises_are_listed(params (string Name, string LastDone, int DaysDone)[] exercises)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("exercises").EnumerateArray()
            .Select(exercise => (
                exercise.GetProperty("name").GetString()!,
                exercise.GetProperty("lastDone").GetString()!,
                exercise.GetProperty("daysDone").GetInt32()))
            .Should().Equal(exercises);

        return Task.CompletedTask;
    }

    /// <param name="exercises">As <c>Exercise reps x kg, reps x kg; Exercise reps x kg</c></param>
    public async Task Then_workout_is_logged(string date, string exercises)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("date").GetString().Should().Be(date);
        DescribeExercises(response.RootElement).Should().Be(exercises);

        var returnedSets = response.RootElement.GetProperty("exercises").EnumerateArray()
            .SelectMany(exercise => exercise.GetProperty("sets").EnumerateArray()
                .Select(set => (
                    set.GetProperty("id").GetInt32(),
                    exercise.GetProperty("exercise").GetString()!,
                    set.GetProperty("reps").GetInt32(),
                    set.GetProperty("weightKg").GetDecimal())))
            .ToList();

        var loggedDate = DateOnly.Parse(date, CultureInfo.InvariantCulture);
        var savedSets = await QueryDatabase(db => db.WorkoutSets
            .AsNoTracking()
            .Where(s => s.Date == loggedDate)
            .OrderBy(s => s.Id)
            .Select(s => new { s.Id, s.Exercise, s.Reps, s.WeightKg })
            .ToListAsync());

        savedSets.Select(s => (s.Id, s.Exercise, s.Reps, s.WeightKg)).Should().Equal(returnedSets);
    }

    public async Task Then_workout_set_is_changed_to(WorkoutSet workoutSet, int reps, decimal weightKg)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("id").GetInt32().Should().Be(workoutSet.Id);
        response.RootElement.GetProperty("exercise").GetString().Should().Be(workoutSet.Exercise);
        response.RootElement.GetProperty("reps").GetInt32().Should().Be(reps);
        response.RootElement.GetProperty("weightKg").GetDecimal().Should().Be(weightKg);

        var savedSet = await QueryDatabase(db => db.WorkoutSets.AsNoTracking().SingleAsync(s => s.Id == workoutSet.Id));
        savedSet.Should().BeEquivalentTo(new { workoutSet.Date, workoutSet.Exercise, Reps = reps, WeightKg = weightKg });
    }

    public async Task Then_workout_sets_are_deleted(params WorkoutSet[] workoutSets)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        var ids = workoutSets.Select(s => s.Id).ToList();

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("deletedIds").EnumerateArray().Select(id => id.GetInt32())
            .Should().BeEquivalentTo(ids);

        (await QueryDatabase(db => db.WorkoutSets.AnyAsync(s => ids.Contains(s.Id)))).Should().BeFalse();
    }

    public async Task Then_workout_sets_are_kept(params WorkoutSet[] workoutSets)
    {
        var ids = workoutSets.Select(s => s.Id).ToList();

        (await QueryDatabase(db => db.WorkoutSets.CountAsync(s => ids.Contains(s.Id)))).Should().Be(ids.Count);
    }

    private static string DescribeExercises(JsonElement day) => string.Join("; ", day
        .GetProperty("exercises").EnumerateArray()
        .Select(exercise =>
        {
            var sets = exercise.GetProperty("sets").EnumerateArray()
                .Select(set => $"{set.GetProperty("reps").GetInt32()} x {set.GetProperty("weightKg").GetDecimal().ToString("0.##", CultureInfo.InvariantCulture)}");

            return $"{exercise.GetProperty("exercise").GetString()} {string.Join(", ", sets)}";
        }));
}
