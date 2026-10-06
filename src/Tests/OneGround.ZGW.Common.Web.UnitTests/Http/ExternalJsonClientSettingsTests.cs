using System;
using OneGround.ZGW.Common.Web.Configuration;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Http;

public class ExternalJsonClientSettingsTests
{
    [Theory]
    [InlineData(5, 5)]
    [InlineData(30, 30)]
    [InlineData(0, 3)]
    [InlineData(-5, 3)]
    [InlineData(31, 3)]
    public void Timeout_OutOfRange_FallsBackToDefault(int configured, int expectedSeconds)
    {
        var settings = new ExternalJsonClientSettings { TimeoutSeconds = configured };

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), settings.Timeout);
    }

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(ExternalJsonClientSettings.MaxAllowedResponseBytes, ExternalJsonClientSettings.MaxAllowedResponseBytes)]
    [InlineData(0, 256 * 1024)]
    [InlineData(-1, 256 * 1024)]
    [InlineData(int.MaxValue, 256 * 1024)]
    public void EffectiveMaxResponseBytes_OutOfRange_FallsBackToDefault(int configured, int expected)
    {
        var settings = new ExternalJsonClientSettings { MaxResponseBytes = configured };

        Assert.Equal(expected, settings.EffectiveMaxResponseBytes);
    }
}
