namespace FatStacks.Core.Calendar;

public sealed class BookCalendar(string timeZoneId, TimeProvider? clock = null)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime((clock ?? TimeProvider.System).GetUtcNow(), zone).DateTime);
    public DateOnly Cutoff => Today.AddDays(-89);
}
