namespace FoodDiary.API.Features.KeepAwake;

public static class KeepAwakeExtensions
{
    public static void AddKeepAwake(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(KeepAwakeOptions.SectionName).Get<KeepAwakeOptions>();

        if (options?.Url is null)
        {
            return;
        }

        if (options.QuietFrom.HasValue != options.QuietUntil.HasValue)
        {
            throw new InvalidOperationException(
                $"{KeepAwakeOptions.SectionName}:QuietFrom and {KeepAwakeOptions.SectionName}:QuietUntil must be set together");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZone, out var timeZone))
        {
            throw new InvalidOperationException(
                $"{KeepAwakeOptions.SectionName}:TimeZone '{options.TimeZone}' is not a known time zone");
        }

        services.AddSingleton(new KeepAwakeSchedule(
            options.Url,
            options.Interval,
            timeZone,
            options.QuietFrom,
            options.QuietUntil));

        services.AddHttpClient(KeepAwakeService.HttpClientName, client => client.Timeout = TimeSpan.FromMinutes(1));
        services.AddHostedService<KeepAwakeService>();
    }
}
