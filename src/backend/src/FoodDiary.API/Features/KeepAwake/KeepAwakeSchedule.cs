namespace FoodDiary.API.Features.KeepAwake;

public sealed class KeepAwakeSchedule(
    Uri url,
    TimeSpan interval,
    TimeZoneInfo timeZone,
    TimeOnly? quietFrom,
    TimeOnly? quietUntil)
{
    public Uri Url => url;

    public TimeSpan Interval => interval;

    public bool IsQuiet(DateTimeOffset now)
    {
        if (quietFrom is not { } from || quietUntil is not { } until)
        {
            return false;
        }

        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        return localTime.IsBetween(from, until);
    }
}
