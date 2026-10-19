using System.Net;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Workouts;

namespace FoodDiary.ComponentTests.Scenarios.Workouts;

public class WorkoutSetsApiTests(InfrastructureFixture infrastructure)
    : BaseTest<WorkoutSetsApiContext>(infrastructure)
{
    private static WorkoutSet Set(string date, string exercise, int reps, decimal weightKg) => new()
    {
        Date = DateOnly.Parse(date),
        Exercise = exercise,
        Reps = reps,
        WeightKg = weightKg
    };

    [Scenario]
    public Task I_can_get_workout_sets_by_date_in_the_order_they_were_logged()
    {
        var squatOnOctober2 = Set("2026-10-02", "Squat", 5, 100m);
        var benchOnOctober1 = Set("2026-10-01", "Bench Press", 10, 60m);
        var pullUpOnOctober1 = Set("2026-10-01", "Pull-up", 8, 0m);
        var squatOnOctober3 = Set("2026-10-03", "Squat", 5, 102.5m);

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_workout_sets(squatOnOctober2, benchOnOctober1, pullUpOnOctober1, squatOnOctober3),
            c => c.When_user_gets_workout_sets("2026-10-01", "2026-10-02"),
            c => c.Then_response_contains_workout_sets(benchOnOctober1, pullUpOnOctober1, squatOnOctober2));
    }

    [Scenario]
    public Task I_can_log_sets_of_an_exercise()
    {
        (int Reps, decimal WeightKg)[] sets = [(10, 60m), (8, 62.5m), (6, 65.004m)];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_user_logs_sets("2026-10-01", "Bench Press", sets),
            c => c.Then_sets_are_logged_with_ids_in_order(),
            c => c.When_user_gets_workout_sets("2026-10-01", "2026-10-01"),
            c => c.Then_logged_sets_are("Bench Press 10 x 60", "Bench Press 8 x 62.5", "Bench Press 6 x 65"),
            c => c.Then_logged_ids_are_the_ones_listed());
    }

    [Scenario]
    public Task I_log_an_exercise_under_the_name_I_already_logged_it_with()
    {
        var benchPress = Set("2026-09-28", "Bench Press", 10, 60m);
        (int Reps, decimal WeightKg)[] sets = [(10, 62.5m)];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_workout_sets(benchPress),
            c => c.When_user_logs_sets("2026-10-01", "  bench PRESS ", sets),
            c => c.When_user_gets_workout_sets("2026-10-01", "2026-10-01"),
            c => c.Then_logged_sets_are("Bench Press 10 x 62.5"));
    }

    [Scenario]
    public Task I_cannot_log_sets_with_reps_out_of_range()
    {
        (int Reps, decimal WeightKg)[] sets = [(10, 60m), (0, 60m)];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_user_logs_sets("2026-10-01", "Bench Press", sets),
            c => c.Then_response_is(HttpStatusCode.BadRequest),
            c => c.When_user_gets_workout_sets("2026-10-01", "2026-10-01"),
            c => c.Then_logged_sets_are());
    }

    [Scenario]
    public Task I_cannot_get_workout_sets_over_more_than_366_days()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_user_gets_workout_sets("2025-01-01", "2026-01-02"),
            c => c.Then_response_is(HttpStatusCode.BadRequest));
    }

    [Scenario]
    public Task I_can_get_exercises_most_recently_done_first()
    {
        WorkoutSet[] workoutSets =
        [
            Set("2026-09-20", "Deadlift", 5, 140m),
            Set("2026-10-01", "Bench Press", 10, 60m),
            Set("2026-10-01", "Squat", 5, 100m),
            Set("2026-09-25", "Bench Press", 10, 57.5m),
            Set("2026-09-28", "Pull-up", 8, 0m)
        ];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_workout_sets(workoutSets),
            c => c.When_user_gets_exercises(),
            c => c.Then_exercises_are("Squat", "Bench Press", "Pull-up", "Deadlift"));
    }

    [Scenario]
    public Task I_can_change_a_set()
    {
        var bench = Set("2026-10-01", "Bench Press", 10, 60m);
        var squat = Set("2026-10-01", "Squat", 5, 100m);

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_workout_sets(bench, squat),
            c => c.When_user_updates_workout_set(bench, "2026-10-02", "squat", 8, 70m),
            c => c.Then_response_is(HttpStatusCode.OK),
            c => c.When_user_gets_workout_sets("2026-10-01", "2026-10-02"),
            c => c.Then_logged_sets_are("Squat 5 x 100", "Squat 8 x 70"));
    }

    [Scenario]
    public Task I_can_delete_a_set()
    {
        var bench = Set("2026-10-01", "Bench Press", 10, 60m);
        var squat = Set("2026-10-01", "Squat", 5, 100m);

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_workout_sets(bench, squat),
            c => c.When_user_deletes_workout_set(bench),
            c => c.Then_response_is(HttpStatusCode.NoContent),
            c => c.When_user_gets_workout_sets("2026-10-01", "2026-10-01"),
            c => c.Then_response_contains_workout_sets(squat));
    }

    [Scenario]
    public Task I_cannot_change_a_set_that_does_not_exist()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_user_updates_workout_set_by_id(404, "2026-10-01", "Squat", 5, 100m),
            c => c.Then_response_is(HttpStatusCode.NotFound));
    }

    [Scenario]
    public Task I_cannot_delete_a_set_that_does_not_exist()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_user_deletes_workout_set_by_id(404),
            c => c.Then_response_is(HttpStatusCode.NotFound));
    }
}
