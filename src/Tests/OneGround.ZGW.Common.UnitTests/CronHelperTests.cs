using System.Globalization;
using OneGround.ZGW.Common.Helpers;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests;

public class CronHelperTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(30, 30)]
    public void CreateRecurringMinuteInterval_WhenValueIsADivisorOf60_KeepsIt(int maxMinutes, int expected)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(maxMinutes);

        // Assert
        Assert.Equal($"*/{expected} * * * *", cron);
    }

    [Theory]
    [InlineData(7, 6)]
    [InlineData(11, 10)]
    [InlineData(13, 12)]
    [InlineData(25, 20)]
    [InlineData(50, 30)] // the ~60-min token / refreshInMinutes=50 case Copilot flagged
    [InlineData(59, 30)]
    public void CreateRecurringMinuteInterval_WhenValueIsNotADivisor_RoundsDownToNearestDivisor(int maxMinutes, int expected)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(maxMinutes);

        // Assert
        Assert.Equal($"*/{expected} * * * *", cron);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CreateRecurringMinuteInterval_WhenValueBelowOne_FallsBackToEveryMinute(int maxMinutes)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(maxMinutes);

        // Assert
        Assert.Equal("*/1 * * * *", cron);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(60)]
    [InlineData(1440)]
    public void CreateRecurringMinuteInterval_WhenValueAboveCeiling_CapsAtThirtyMinutes(int maxMinutes)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(maxMinutes);

        // Assert
        Assert.Equal("*/30 * * * *", cron);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(13)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(59)]
    public void CreateRecurringMinuteInterval_AlwaysProducesAnEvenCadence(int maxMinutes)
    {
        // The chosen step must divide 60 evenly, otherwise "*/N" bunches runs around the hour
        // boundary. Parse N back out of the cron string and assert 60 % N == 0.
        var cron = CronHelper.CreateRecurringMinuteInterval(maxMinutes);

        var step = int.Parse(cron.Substring("*/".Length, cron.IndexOf(' ') - "*/".Length), CultureInfo.InvariantCulture);
        Assert.Equal(0, 60 % step);
    }

    [Theory]
    [InlineData(7, 6)]
    [InlineData(50, 30)]
    [InlineData(59, 30)]
    public void CreateRecurringMinuteInterval_NeverExceedsTheRequestedMaximum(int maxMinutes, int expectedAtMost)
    {
        // Rounding down guarantees the gap between runs never exceeds the requested maximum,
        // so the token is always renewed before expiry.
        var cron = CronHelper.CreateRecurringMinuteInterval(maxMinutes);

        var step = int.Parse(cron.Substring("*/".Length, cron.IndexOf(' ') - "*/".Length), CultureInfo.InvariantCulture);
        Assert.True(step <= maxMinutes);
        Assert.Equal(expectedAtMost, step);
    }

    [Fact]
    public void CreateRecurringMinuteInterval_ProducesAStepMinuteCron_NotAOneTimeInstant()
    {
        // A recurring cron uses a "*/N" minute step and wildcards for the remaining fields, so it
        // fires repeatedly. A one-time cron pins concrete minute/hour/day/month values. This guards
        // against accidentally regressing back to the fragile one-time behaviour.
        var recurring = CronHelper.CreateRecurringMinuteInterval(15);

        Assert.StartsWith("*/", recurring);
        Assert.EndsWith(" * * * *", recurring);
    }
}
