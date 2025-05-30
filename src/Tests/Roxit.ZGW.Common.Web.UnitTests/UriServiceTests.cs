using System;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;
using Xunit;

namespace Roxit.ZGW.Common.Web.UnitTests;

public class UriServiceTests
{
    private readonly Mock<IServiceDiscovery> _serviceEndpoints = new Mock<IServiceDiscovery>();

    class SomeEntity : IUrlEntity
    {
        public string Url => $"/entity/queryparameter?key=value";
    }

    private UriService MockUriService(string scheme = "http", int port = 80)
    {
        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Host).Returns(new HostString("localhost", port));
        request.Setup(r => r.Scheme).Returns(scheme);
        request.Setup(r => r.Path).Returns(new PathString("/api/v1"));

        var apiVersionFeature = new Mock<IApiVersioningFeature>();
        apiVersionFeature.Setup(f => f.RequestedApiVersion).Returns(new ApiVersion(1, 0));

        var featureCollection = new Mock<IFeatureCollection>();
        featureCollection.Setup(f => f.Get<IApiVersioningFeature>()).Returns(apiVersionFeature.Object);

        var context = new Mock<HttpContext>();
        context.Setup(c => c.Request).Returns(request.Object);
        context.Setup(c => c.Features).Returns(featureCollection.Object);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(context.Object);

        return new UriService(httpContextAccessor.Object, _serviceEndpoints.Object);
    }

    [Fact]
    public void UriService_Returns_NonEscapedUrl()
    {
        var uri = MockUriService().GetUri(new SomeEntity());

        Assert.Equal("http://localhost/api/v1/entity/queryparameter?key=value", uri);
    }

    [Fact]
    public void UriService_Returns_NoExplicitPortFor80()
    {
        var uri = MockUriService("http", 80).GetUri(new SomeEntity());

        Assert.Equal("http://localhost/api/v1/entity/queryparameter?key=value", uri);
    }

    [Fact]
    public void UriService_Returns_ExplicitPortForHttpNon80()
    {
        var uri = MockUriService("http", 123).GetUri(new SomeEntity());

        Assert.Equal("http://localhost:123/api/v1/entity/queryparameter?key=value", uri);
    }

    [Fact]
    public void UriService_Returns_NoExplicitPortForHttps443()
    {
        var uri = MockUriService("https", 443).GetUri(new SomeEntity());

        Assert.Equal("https://localhost/api/v1/entity/queryparameter?key=value", uri);
    }

    [Fact]
    public void UriService_Returns_ExplicitPortForHttpsNon443()
    {
        var uri = MockUriService("https", 123).GetUri(new SomeEntity());

        Assert.Equal("https://localhost:123/api/v1/entity/queryparameter?key=value", uri);
    }

    [Fact]
    public void UriService_Returns_DifferentServiceRole()
    {
        _serviceEndpoints.Setup(s => s.GetApi(ServiceRoleName.ZTC)).Returns(new Uri("https://catalogi-services.dev.local/api/v1"));

        var uri = MockUriService("https", 443).GetUri(ServiceRoleName.ZTC, new SomeEntity());

        Assert.Equal("https://catalogi-services.dev.local/api/v1/entity/queryparameter?key=value", uri);
    }
}
