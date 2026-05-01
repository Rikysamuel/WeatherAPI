namespace WeatherApi.Core.Common;

public static class DateTimeUtil
{

    public static DateTimeOffset ConvertToSGTime(DateTimeOffset utcTime)
    {
        return utcTime.ToOffset(TimeSpan.FromHours(8));
    }
}