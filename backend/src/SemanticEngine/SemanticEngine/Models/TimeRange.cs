namespace SemanticEngine.Models;

/// <summary>
/// Khoảng thời gian cho time-series query.
/// Hỗ trợ preset ("last7days") hoặc absolute range.
/// </summary>
public sealed class TimeRange
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    /// <summary>
    /// Preset shortcuts: today, yesterday, last7days, last30days,
    /// thisMonth, lastMonth, thisQuarter, thisYear, lastYear
    /// </summary>
    public string? Preset { get; init; }

    public (DateTimeOffset From, DateTimeOffset To) Resolve()
    {
        if (Preset is not null)
        {
            var now = DateTimeOffset.UtcNow;
            var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

            return Preset.ToLowerInvariant() switch
            {
                "today"         => (today, today.AddDays(1).AddTicks(-1)),
                "yesterday"     => (today.AddDays(-1), today.AddTicks(-1)),
                "last7days"     => (today.AddDays(-7), now),
                "last14days"    => (today.AddDays(-14), now),
                "last30days"    => (today.AddDays(-30), now),
                "last90days"    => (today.AddDays(-90), now),
                "thisweek"      => (today.AddDays(-(int)now.DayOfWeek), now),
                "thismonth"     => (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero), now),
                "lastmonth"     => (
                    new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-1),
                    new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(-1)
                ),
                "thisquarter"   => GetThisQuarter(now),
                "thisyear"      => (new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero), now),
                "lastyear"      => (
                    new DateTimeOffset(now.Year - 1, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(-1)
                ),
                _ => (DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
            };
        }
        return (From ?? DateTimeOffset.MinValue, To ?? DateTimeOffset.MaxValue);
    }

    private static (DateTimeOffset, DateTimeOffset) GetThisQuarter(DateTimeOffset now)
    {
        var quarterStartMonth = ((now.Month - 1) / 3) * 3 + 1;
        var start = new DateTimeOffset(now.Year, quarterStartMonth, 1, 0, 0, 0, TimeSpan.Zero);
        return (start, now);
    }
}
