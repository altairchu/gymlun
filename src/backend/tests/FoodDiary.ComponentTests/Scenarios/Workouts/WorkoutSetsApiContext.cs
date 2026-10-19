using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.API.Features.Workouts;
using FoodDiary.API.Features.Workouts.Contracts;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Workouts;
using JetBrains.Annotations;

namespace FoodDiary.ComponentTests.Scenarios.Workouts;

[UsedImplicitly]
public class WorkoutSetsApiContext(FoodDiaryWebApplicationFactory factory) : BaseContext(factory)
{
    private HttpResponseMessage _response = null!;
    private GetWorkoutSetsResponse? _getWorkoutSetsResponse;
    private GetExercisesResponse? _getExercisesResponse;
    private LogWorkoutSetsResponse? _logWorkoutSetsResponse;

    public async Task Given_existing_workout_sets(params WorkoutSet[] workoutSets)
    {
        foreach (var workoutSet in workoutSets)
        {
            await Factory.SeedDataAsync([workoutSet]);
        }
    }

    public async Task When_user_gets_workout_sets(string from, string to)
    {
        _response = await ApiClient.GetAsync($"api/v1/workout-sets?from={from}&to={to}");

        _getWorkoutSetsResponse = _response.IsSuccessStatusCode
            ? await _response.Content.ReadFromJsonAsync<GetWorkoutSetsResponse>()
            : null;
    }

    public async Task When_user_gets_exercises()
    {
        _getExercisesResponse = await ApiClient.GetFromJsonAsync<GetExercisesResponse>("api/v1/workout-sets/exercises");
    }

    public async Task When_user_logs_sets(string date, string exercise, params (int Reps, decimal WeightKg)[] sets)
    {
        var request = new LogWorkoutSetsRequest(
            DateOnly.Parse(date),
            exercise,
            sets.Select(set => new LogWorkoutSetsRequestSet(set.Reps, set.WeightKg)).ToList());

        _response = await ApiClient.PostAsJsonAsync("api/v1/workout-sets", request);

        _logWorkoutSetsResponse = _response.IsSuccessStatusCode
            ? await _response.Content.ReadFromJsonAsync<LogWorkoutSetsResponse>()
            : null;
    }

    public async Task When_user_updates_workout_set(WorkoutSet workoutSet, string date, string exercise, int reps, decimal weightKg)
    {
        await When_user_updates_workout_set_by_id(workoutSet.Id, date, exercise, reps, weightKg);
    }

    public async Task When_user_updates_workout_set_by_id(int id, string date, string exercise, int reps, decimal weightKg)
    {
        _response = await ApiClient.PutAsJsonAsync(
            $"api/v1/workout-sets/{id}",
            new UpdateWorkoutSetRequest(DateOnly.Parse(date), exercise, reps, weightKg));
    }

    public async Task When_user_deletes_workout_set(WorkoutSet workoutSet)
    {
        await When_user_deletes_workout_set_by_id(workoutSet.Id);
    }

    public async Task When_user_deletes_workout_set_by_id(int id)
    {
        _response = await ApiClient.DeleteAsync($"api/v1/workout-sets/{id}");
    }

    public Task Then_response_contains_workout_sets(params WorkoutSet[] workoutSets)
    {
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
        _getWorkoutSetsResponse!.WorkoutSets.Should().Equal(workoutSets.Select(s => s.ToWorkoutSetItem()));
        return Task.CompletedTask;
    }

    /// <param name="sets">Each set in order as <c>Exercise reps x kg</c></param>
    public Task Then_logged_sets_are(params string[] sets)
    {
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
        _getWorkoutSetsResponse!.WorkoutSets
            .Select(s => $"{s.Exercise} {s.Reps} x {s.WeightKg.ToString("0.##", CultureInfo.InvariantCulture)}")
            .Should().Equal(sets);
        return Task.CompletedTask;
    }

    public Task Then_sets_are_logged_with_ids_in_order()
    {
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
        _logWorkoutSetsResponse!.Ids.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        return Task.CompletedTask;
    }

    public Task Then_logged_ids_are_the_ones_listed()
    {
        _getWorkoutSetsResponse!.WorkoutSets.Select(s => s.Id).Should().Equal(_logWorkoutSetsResponse!.Ids);
        return Task.CompletedTask;
    }

    public Task Then_exercises_are(params string[] exercises)
    {
        _getExercisesResponse!.Exercises.Should().Equal(exercises);
        return Task.CompletedTask;
    }

    public Task Then_response_is(HttpStatusCode statusCode)
    {
        _response.StatusCode.Should().Be(statusCode);
        return Task.CompletedTask;
    }
}
