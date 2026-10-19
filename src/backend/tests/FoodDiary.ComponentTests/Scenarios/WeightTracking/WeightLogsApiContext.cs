using System.Net;
using System.Net.Http.Json;
using FoodDiary.API.Features.WeightTracking;
using FoodDiary.API.Features.WeightTracking.Contracts;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.WeightTracking;
using JetBrains.Annotations;

namespace FoodDiary.ComponentTests.Scenarios.WeightTracking;

[UsedImplicitly]
public class WeightLogsApiContext(FoodDiaryWebApplicationFactory factory) : BaseContext(factory)
{
    private GetWeightLogsResponse? _getWeightLogsResponse;
    private HttpResponseMessage? _addWeightLogResponse;
    private HttpResponseMessage _changeWeightLogResponse = null!;

    public Task Given_existing_weightLogs(params WeightLog[] weightLogs)
    {
        return Factory.SeedDataAsync(weightLogs);
    }

    public async Task When_user_gets_weightLogs(string from, string to)
    {
        _getWeightLogsResponse = await ApiClient
            .GetFromJsonAsync<GetWeightLogsResponse>($"api/weight-logs?from={from}&to={to}");
    }

    public async Task When_user_adds_weightLog(WeightLog weightLog)
    {
        _addWeightLogResponse = await ApiClient.PostAsJsonAsync("api/weight-logs", weightLog.ToWeightLogBody());
    }

    public async Task When_user_updates_weightLog(string date, decimal value)
    {
        _changeWeightLogResponse = await ApiClient.PutAsJsonAsync(
            $"api/weight-logs/{date}",
            new UpdateWeightLogBody { Value = value });
    }

    public async Task When_user_deletes_weightLog(string date)
    {
        _changeWeightLogResponse = await ApiClient.DeleteAsync($"api/weight-logs/{date}");
    }

    public Task Then_response_contains_weightLogs(params WeightLog[] items)
    {
        _getWeightLogsResponse?.WeightLogs.Should()
            .ContainInOrder(items.Select(wl => wl.ToWeightLogItem()))
            .And.HaveSameCount(items);
        return Task.CompletedTask;
    }

    public Task Then_weight_is_successfully_saved()
    {
        _addWeightLogResponse?.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    public Task Then_weight_log_is_successfully_changed()
    {
        _changeWeightLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    public Task Then_weight_log_is_not_found()
    {
        _changeWeightLogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        return Task.CompletedTask;
    }
}
