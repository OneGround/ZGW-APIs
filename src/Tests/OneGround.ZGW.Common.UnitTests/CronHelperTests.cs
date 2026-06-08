using OneGround.ZGW.Common.Helpers;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests;

public class CronHelperTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(59)]
    public void CreateRecurringMinuteInterval_WithValidInterval_ReturnsRecurringStepCron(int interval)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(interval);

        // Assert
        Assert.Equal($"*/{interval} * * * *", cron);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    public void CreateRecurringMinuteInterval_WithIntervalBelowOne_ClampsToOne(int interval, int expected)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(interval);

        // Assert
        Assert.Equal($"*/{expected} * * * *", cron);
    }

    [Theory]
    [InlineData(60, 59)]
    [InlineData(1440, 59)]
    public void CreateRecurringMinuteInterval_WithIntervalAboveFiftyNine_ClampsToFiftyNine(int interval, int expected)
    {
        // Act
        var cron = CronHelper.CreateRecurringMinuteInterval(interval);

        // Assert
        Assert.Equal($"*/{expected} * * * *", cron);
    }

    [Fact]
    public void CreateRecurringMinuteInterval_AlwaysProducesAStepMinuteCron_NotAOneTimeInstant()
    {
        // A recurring cron uses a "*/N" minute step and wildcards for the remaining fields, so it
        // fires repeatedly. A one-time cron pins concrete minute/hour/day/month values. This guards
        // against accidentally regressing back to the fragile one-time behaviour.
        var recurring = CronHelper.CreateRecurringMinuteInterval(15);

        Assert.StartsWith("*/", recurring);
        Assert.EndsWith(" * * * *", recurring);
    }
}
