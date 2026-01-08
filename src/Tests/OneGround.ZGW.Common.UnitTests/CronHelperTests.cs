using System;
using OneGround.ZGW.Common.Helpers;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests;

public class CronHelperTests
{
    [Fact]
    public void CreateOneTimeCron_WithPositiveMinutes_ReturnsCorrectCronExpression()
    {
        // Arrange
        var minutesFromNow = 30;
        var expectedTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }

    [Fact]
    public void CreateOneTimeCron_WithZeroMinutes_ReturnsCurrentTimeCronExpression()
    {
        // Arrange
        var minutesFromNow = 0;
        var expectedTime = DateTime.UtcNow;

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }

    [Fact]
    public void CreateOneTimeCron_WithNegativeMinutes_ReturnsCorrectCronExpression()
    {
        // Arrange
        var minutesFromNow = -15;
        var expectedTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }

    [Fact]
    public void CreateOneTimeCron_CrossingHourBoundary_ReturnsCorrectCronExpression()
    {
        // Arrange
        var minutesFromNow = 120; // 2 hours
        var expectedTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }

    [Fact]
    public void CreateOneTimeCron_CrossingDayBoundary_ReturnsCorrectCronExpression()
    {
        // Arrange
        var minutesFromNow = 1500; // ~25 hours
        var expectedTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }

    [Fact]
    public void CreateOneTimeCron_CrossingMonthBoundary_ReturnsCorrectCronExpression()
    {
        // Arrange
        var minutesFromNow = 50000; // ~34 days
        var expectedTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }

    [Fact]
    public void CreateOneTimeCron_ReturnsCorrectCronFormat()
    {
        // Arrange
        var minutesFromNow = 10;

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert - Verify the format: "Minute Hour DayOfMonth Month DayOfWeek"
        var parts = result.Split(' ');
        Assert.Equal(5, parts.Length); // Cron should have 5 parts
        Assert.Equal("*", parts[4]); // DayOfWeek should always be *
    }

    [Fact]
    public void CreateOneTimeCron_WithLargeMinutesValue_ReturnsCorrectCronExpression()
    {
        // Arrange
        var minutesFromNow = 525600; // 1 year in minutes
        var expectedTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // Act
        var result = CronHelper.CreateOneTimeCron(minutesFromNow);

        // Assert
        var expectedCron = $"{expectedTime.Minute} {expectedTime.Hour} {expectedTime.Day} {expectedTime.Month} *";
        Assert.Equal(expectedCron, result);
    }
}
