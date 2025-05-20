using System;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Services;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests.Services;

public class AppSettingsEndPointsTests
{
    [Fact]
    public void GetApi_GivenServiceNameZrc_ReturnsUrlFromAppSettings()
    {
        var appSettings = new EndpointConfiguration { ZRC = new DiscoverableService() { Api = "https://zrc-api.com/" } };

        var optionsMonitorMock = new Mock<IOptionsMonitor<EndpointConfiguration>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(appSettings);
        var sut = new EndpointsAppSettingsServiceDiscovery(optionsMonitorMock.Object);

        var result = sut.GetApi("ZRC");

        Assert.Equal("https://zrc-api.com/", result.ToString());
    }

    [Fact]
    public void GetApi_GivenServiceNameZtc_ReturnsUrlFrom_AppSettings()
    {
        var appSettings = new EndpointConfiguration { ZTC = new DiscoverableService() { Api = "https://ztc-api.com/" } };

        var optionsMonitorMock = new Mock<IOptionsMonitor<EndpointConfiguration>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(appSettings);
        var sut = new EndpointsAppSettingsServiceDiscovery(optionsMonitorMock.Object);

        var result = sut.GetApi("ZTC");

        Assert.Equal("https://ztc-api.com/", result.ToString());
    }

    [Fact]
    public void GetApi_GivenServiceNameUnkown_ThrowsException()
    {
        var appSettings = new EndpointConfiguration { ZTC = new DiscoverableService() { Api = "https://ztc-api.com/" } };

        var optionsMonitorMock = new Mock<IOptionsMonitor<EndpointConfiguration>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(appSettings);
        var sut = new EndpointsAppSettingsServiceDiscovery(optionsMonitorMock.Object);

        Assert.Throws<InvalidOperationException>(() => sut.GetApi("Unknown"));
    }
}
