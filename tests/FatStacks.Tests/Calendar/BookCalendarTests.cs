using FatStacks.Core.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Calendar;

[TestClass]
public sealed class BookCalendarTests
{
    [TestMethod]
    public void BookZoneAndInclusiveNinetyDaysDoNotUseDeviceZone()
    {
        var calendar = new BookCalendar("Pacific Standard Time", new FixedClock(new(2026, 3, 8, 7, 30, 0, TimeSpan.Zero)));
        Assert.AreEqual(new DateOnly(2026, 3, 7), calendar.Today);
        Assert.AreEqual(90, calendar.Today.DayNumber - calendar.Cutoff.DayNumber + 1);
        Assert.AreEqual(new DateOnly(2026, 3, 8), new BookCalendar("UTC", new FixedClock(new(2026, 3, 8, 7, 30, 0, TimeSpan.Zero))).Today);
    }
    [TestMethod]
    public void DaylightSavingRepeatedAndSkippedHoursKeepAccountingDate()
    {
        foreach (var utc in new[] { new DateTimeOffset(2026, 3, 8, 9, 59, 0, TimeSpan.Zero), new(2026, 3, 8, 10, 1, 0, TimeSpan.Zero) })
            Assert.AreEqual(new DateOnly(2026, 3, 8), new BookCalendar("Pacific Standard Time", new FixedClock(utc)).Today);
        foreach (var hour in new[] { 8, 9 })
            Assert.AreEqual(new DateOnly(2026, 11, 1), new BookCalendar("Pacific Standard Time", new FixedClock(new(2026, 11, 1, hour, 30, 0, TimeSpan.Zero))).Today);
    }
    private sealed class FixedClock(DateTimeOffset utc) : TimeProvider { public override DateTimeOffset GetUtcNow() => utc; }
}
