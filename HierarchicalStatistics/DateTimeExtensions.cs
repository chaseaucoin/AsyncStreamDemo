public static class DateTimeExtensions
{
    /// <summary>
    /// Returns the DateTime at 00:00 on the first day of the month.
    /// </summary>
    /// <param name="dateTime">The original DateTime</param>
    /// <returns>DateTime at the beginning of the month</returns>
    public static DateTime BeginningOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Returns the DateTime at 00:00 on the first day of the week.
    /// The week starts on Monday.
    /// </summary>
    /// <param name="dateTime">The original DateTime</param>
    /// <returns>DateTime at the beginning of the week</returns>
    public static DateTime BeginningOfWeek(this DateTime dateTime)
    {
        var diff = dateTime.DayOfWeek - DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Returns the DateTime at 00:00 on the same day.
    /// </summary>
    /// <param name="dateTime">The original DateTime</param>
    /// <returns>DateTime at the beginning of the day</returns>
    public static DateTime BeginningOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Returns the DateTime at 00:00 on the first day of the quarter.
    /// </summary>
    /// <param name="dateTime">The original DateTime</param>
    /// <returns>DateTime at the beginning of the quarter</returns>
    public static DateTime BeginningOfQuarter(this DateTime dateTime)
    {
        int quarterNumber = (dateTime.Month - 1) / 3 + 1;
        int startMonth = (quarterNumber - 1) * 3 + 1;
        return new DateTime(dateTime.Year, startMonth, 1);
    }

    /// <summary>
    /// Returns the DateTime at 00:00 on the first day of the year.
    /// </summary>
    /// <param name="dateTime">The original DateTime</param>
    /// <returns>DateTime at the beginning of the year</returns>
    public static DateTime BeginningOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }
}
