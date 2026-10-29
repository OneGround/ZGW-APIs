using System;
using OneGround.ZGW.Zaken.Web.Models.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_7;

public class DateTimeWindowTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Without_a_value_there_is_no_window(string value)
    {
        Assert.Null(DateTimeWindow.FromString(value));
    }

    [Fact]
    public void A_date_without_time_covers_the_whole_day()
    {
        var window = DateTimeWindow.FromString("2026-10-23");

        Assert.Equal(new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc), window.From);
        Assert.Equal(new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc), window.Until);
    }

    [Theory]
    [InlineData("2026-10-23T07:33:48Z")]
    [InlineData("2026-10-23T07:33:48.987Z")]
    [InlineData("2026-10-23T09:33:48+02:00")]
    [InlineData("2026-10-23 07:33:48")]
    public void A_date_with_time_covers_that_second(string value)
    {
        var window = DateTimeWindow.FromString(value);

        Assert.Equal(new DateTime(2026, 10, 23, 7, 33, 48, DateTimeKind.Utc), window.From);
        Assert.Equal(new DateTime(2026, 10, 23, 7, 33, 49, DateTimeKind.Utc), window.Until);
    }

    [Fact]
    public void A_midnight_with_time_is_still_a_single_second()
    {
        var window = DateTimeWindow.FromString("2026-10-23T00:00:00Z");

        Assert.Equal(TimeSpan.FromSeconds(1), window.Until - window.From);
    }
}
