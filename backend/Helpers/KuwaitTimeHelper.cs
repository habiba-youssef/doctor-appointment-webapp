namespace DoctorsAppointment.Api.Helpers;

public static class KuwaitTimeHelper
{
    // Define Kuwait's time zone
    private static readonly TimeZoneInfo KuwaitZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuwait");

    // Return current Kuwait date and time
    public static DateTime GetNow(TimeProvider clock)
    {
        var utcNow = clock.GetUtcNow();
        var kuwaitNow = TimeZoneInfo.ConvertTime(utcNow, KuwaitZone);

        return kuwaitNow.DateTime;
    }

    // Return today's date in Kuwait
    public static DateOnly GetToday(TimeProvider clock)
    {
        return DateOnly.FromDateTime(GetNow(clock));
    }
}
