using System;
using OneGround.ZGW.Common.Helpers;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests;

public class ProfileHelperTests
{
    private const string UtcFormat = "yyyy-MM-ddTHH:mm:ssZ";

    [Fact]
    public void ProfileHelper_maps_string_to_datetime_correctly()
    {
        var utcDateString = "2020-01-10T12:00:00Z";

        var utc = ProfileHelper.DateTimeFromString(utcDateString);

        Assert.NotNull(utc);
        Assert.Equal(utcDateString, utc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Fact]
    public void ProfileHelper_maps_datetime_to_string_correctly()
    {
        var utcDateTime = DateTime.UtcNow;
        var regularDateTime = DateTime.Now;

        var utc = ProfileHelper.StringDateFromDateTime(utcDateTime, withTime: true);
        var regular = ProfileHelper.StringDateFromDateTime(regularDateTime, withTime: true);

        var expectedUtcString = utcDateTime.ToString(UtcFormat);
        var expectedRegularString = regularDateTime.ToUniversalTime().ToString(UtcFormat);

        Assert.Equal(expectedUtcString, utc);
        Assert.Equal(expectedRegularString, regular);
    }
}
