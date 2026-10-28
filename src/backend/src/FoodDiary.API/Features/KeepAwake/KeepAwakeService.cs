namespace FoodDiary.API.Features.KeepAwake;

/// <summary>
/// Requests the app's own public URL on a schedule, for hosts that put the app to sleep when no traffic reaches it.
/// Once the host has put it to sleep, nothing here runs until the next visitor wakes it.
/// </summary>
public class KeepAwakeService(
    IHttpClientFactory httpClientFactory,
    KeepAwakeSchedule schedule,
    TimeProvider timeProvider,
    ILogger<KeepAwakeService> logger) : BackgroundService
{
    public const string HttpClientName = "KeepAwake";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(schedule.Interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!schedule.IsQuiet(timeProvider.GetUtcNow()))
            {
                await Ping(stoppingToken);
            }
        }
    }

    private async Task Ping(CancellationToken stoppingToken)
    {
        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName)
                .GetAsync(schedule.Url, stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Keep-awake request to {Url} answered {StatusCode}", schedule.Url,
                    (int)response.StatusCode);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException &&
                                          !stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Keep-awake request to {Url} failed", schedule.Url);
        }
    }
}
