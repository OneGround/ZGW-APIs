using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakBesluitBesluitResolverTests
{
    private const string BesluitUrl = "https://brc.test/besluiten/77777777-7777-7777-7777-777777777777";

    private static readonly IReadOnlyDictionary<string, object> NothingResolved = new Dictionary<string, object>();
    private static readonly IReadOnlySet<string> BesluitRequested = new HashSet<string> { "besluit" };

    [Fact]
    public async Task ResolveAsync_BesluitFound_ReturnsTheBesluitOfBrc()
    {
        var besluit = new BesluitResponseDto { Url = BesluitUrl };
        var serviceAgentMock = new Mock<IUserAuthBesluitenServiceAgent>();
        serviceAgentMock.Setup(a => a.GetBesluitByUrlAsync(BesluitUrl)).ReturnsAsync(new ServiceAgentResponse<BesluitResponseDto>(besluit));

        var resolver = new ZaakBesluitBesluitResolver(serviceAgentMock.Object, ExpandCaches.Besluit());

        var result = await resolver.ResolveAsync(new ZaakBesluitResponseDto { Besluit = BesluitUrl }, NothingResolved, BesluitRequested);

        Assert.Same(besluit, result);
    }

    [Fact]
    public async Task ResolveAsync_SameBesluitTwice_CallsBrcOnce()
    {
        var serviceAgentMock = new Mock<IUserAuthBesluitenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetBesluitByUrlAsync(BesluitUrl))
            .ReturnsAsync(new ServiceAgentResponse<BesluitResponseDto>(new BesluitResponseDto { Url = BesluitUrl }));

        var resolver = new ZaakBesluitBesluitResolver(serviceAgentMock.Object, ExpandCaches.Besluit());
        var entity = new ZaakBesluitResponseDto { Besluit = BesluitUrl };

        await resolver.ResolveAsync(entity, NothingResolved, BesluitRequested);
        await resolver.ResolveAsync(entity, NothingResolved, BesluitRequested);

        serviceAgentMock.Verify(a => a.GetBesluitByUrlAsync(BesluitUrl), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_BrcCallFails_ThrowsExpandExternalServiceException()
    {
        var serviceAgentMock = new Mock<IUserAuthBesluitenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetBesluitByUrlAsync(BesluitUrl))
            .ReturnsAsync(new ServiceAgentResponse<BesluitResponseDto>(new ErrorResponse { Status = 502 }, null));

        var resolver = new ZaakBesluitBesluitResolver(serviceAgentMock.Object, ExpandCaches.Besluit());

        var ex = await Assert.ThrowsAsync<ExpandExternalServiceException>(() =>
            resolver.ResolveAsync(new ZaakBesluitResponseDto { Besluit = BesluitUrl }, NothingResolved, BesluitRequested)
        );

        Assert.Equal("BRC", ex.ServiceName);
        Assert.Equal(BesluitUrl, ex.ServiceUrl);
        Assert.Equal(502, ex.StatusCode);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(404)]
    public async Task ResolveAsync_BesluitNotAvailableToTheCaller_ResolvesToNullInsteadOfFailingTheRequest(int status)
    {
        var serviceAgentMock = new Mock<IUserAuthBesluitenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetBesluitByUrlAsync(BesluitUrl))
            .ReturnsAsync(new ServiceAgentResponse<BesluitResponseDto>(new ErrorResponse { Status = status }, null));

        var resolver = new ZaakBesluitBesluitResolver(serviceAgentMock.Object, ExpandCaches.Besluit());

        var result = await resolver.ResolveAsync(new ZaakBesluitResponseDto { Besluit = BesluitUrl }, NothingResolved, BesluitRequested);

        Assert.Null(result);
    }

    [Fact]
    public void Path_IsBesluit_AndHasNoParent()
    {
        var resolver = new ZaakBesluitBesluitResolver(Mock.Of<IUserAuthBesluitenServiceAgent>(), ExpandCaches.Besluit());

        Assert.Equal("besluit", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
