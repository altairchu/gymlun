namespace FoodDiary.API.Features.KeepAwake;

public class KeepAwakeOptions
{
    public const string SectionName = "KeepAwake";

    public Uri? Url { get; init; }

    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(10);

    public string TimeZone { get; init; } = "UTC";

    public TimeOnly? QuietFrom { get; init; }

    public TimeOnly? QuietUntil { get; init; }
}
