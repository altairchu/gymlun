using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.WeightTracking;

namespace FoodDiary.ComponentTests.Scenarios.WeightTracking;

public class WeightLogsApiTests(InfrastructureFixture infrastructure)
    : BaseTest<WeightLogsApiContext>(infrastructure)
{
    private static WeightLog[] GivenWeightLogs() =>
    [
        new() { Date = DateOnly.Parse("2024-10-01"), Weight = 75.6m },
        new() { Date = DateOnly.Parse("2024-10-02"), Weight = 76.5m },
        new() { Date = DateOnly.Parse("2024-10-03"), Weight = 76.2m },
        new() { Date = DateOnly.Parse("2024-10-04"), Weight = 75.9m }
    ];

    [Scenario]
    public Task I_can_get_weight_logs()
    {
        var weightLogs = GivenWeightLogs();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_weightLogs(GivenWeightLogs()),
            c => c.When_user_gets_weightLogs("2024-10-02", "2024-10-03"),
            c => c.Then_response_contains_weightLogs(weightLogs[2], weightLogs[1]));
    }

    [Scenario]
    public Task I_can_log_my_weight()
    {
        var weightLogs = GivenWeightLogs();
        var newWeightLog = new WeightLog { Date = DateOnly.Parse("2024-10-05"), Weight = 76.3m };
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_weightLogs(weightLogs),
            c => c.When_user_adds_weightLog(newWeightLog),
            c => c.Then_weight_is_successfully_saved(),
            c => c.When_user_gets_weightLogs("2024-10-03", "2024-10-05"),
            c => c.Then_response_contains_weightLogs(newWeightLog, weightLogs[3], weightLogs[2]));
    }

    [Scenario]
    public Task I_can_change_my_weight_on_a_date()
    {
        var weightLogs = GivenWeightLogs();
        var changedWeightLog = new WeightLog { Date = DateOnly.Parse("2024-10-03"), Weight = 75.8m };

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_weightLogs(weightLogs),
            c => c.When_user_updates_weightLog("2024-10-03", 75.8m),
            c => c.Then_weight_log_is_successfully_changed(),
            c => c.When_user_gets_weightLogs("2024-10-02", "2024-10-04"),
            c => c.Then_response_contains_weightLogs(weightLogs[3], changedWeightLog, weightLogs[1]));
    }

    [Scenario]
    public Task I_can_delete_my_weight_on_a_date()
    {
        var weightLogs = GivenWeightLogs();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_weightLogs(weightLogs),
            c => c.When_user_deletes_weightLog("2024-10-03"),
            c => c.Then_weight_log_is_successfully_changed(),
            c => c.When_user_gets_weightLogs("2024-10-02", "2024-10-04"),
            c => c.Then_response_contains_weightLogs(weightLogs[3], weightLogs[1]));
    }

    [Scenario]
    public Task I_cannot_change_weight_on_a_date_without_one()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_weightLogs(GivenWeightLogs()),
            c => c.When_user_updates_weightLog("2024-10-05", 75.8m),
            c => c.Then_weight_log_is_not_found());
    }

    [Scenario]
    public Task I_cannot_delete_weight_on_a_date_without_one()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_existing_weightLogs(GivenWeightLogs()),
            c => c.When_user_deletes_weightLog("2024-10-05"),
            c => c.Then_weight_log_is_not_found());
    }
}