using OneGround.ZGW.Common.Helpers;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests;

public class CronHelperTests
{
    [Theory]
    [InlineData(0, "*/1 * * * *")] // minutes <= 0, should default to 1
    [InlineData(-5, "*/1 * * * *")] // negative minutes, should default to 1
    [InlineData(1, "*/1 * * * *")] // 1 minute
    [InlineData(5, "*/5 * * * *")] // less than 60 minutes
    [InlineData(59, "*/59 * * * *")] // just below 60 minutes
    [InlineData(60, "0 */1 * * *")] // exactly 1 hour
    [InlineData(120, "0 */2 * * *")] // exactly 2 hours
    [InlineData(180, "0 */3 * * *")] // exactly 3 hours
    [InlineData(1440, "0 0 */1 * *")] // exactly 1 day
    [InlineData(2880, "0 0 */2 * *")] // exactly 2 days
    [InlineData(1500, "0 0 */1 * *")] // more than 1 day, not exactly multiple
    public void CreateCronForIntervalMinutes_ReturnsExpectedCron(int minutes, string expected)
    {
        var result = CronHelper.CreateCronForIntervalMinutes(minutes);
        Assert.Equal(expected, result);
    }
}
