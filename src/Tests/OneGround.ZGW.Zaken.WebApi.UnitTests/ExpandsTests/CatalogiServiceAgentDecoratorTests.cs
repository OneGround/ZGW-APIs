using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The first ZTC call of a request is made as the caller; once ZTC has accepted the caller, the rest of that request uses the agent with
/// the shared response cache. Never the other way around, so a caller without access cannot be served from that cache.
/// </summary>
public class CatalogiServiceAgentDecoratorTests
{
    private const string ZaakTypeUrl1 = "https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111";
    private const string ZaakTypeUrl2 = "https://ztc.test/zaaktypen/22222222-2222-2222-2222-222222222222";
    private const string RolTypeUrl = "https://ztc.test/roltypen/33333333-3333-3333-3333-333333333333";

    private static ServiceAgentResponse<T> Ok<T>(T response) => new(response);

    private static ServiceAgentResponse<T> Failed<T>(int status) => new(new ErrorResponse { Status = status });

    private static (Mock<IUserAuthCatalogiServiceAgent> Caller, Mock<ICatalogiServiceAgent> Cached, CatalogiServiceAgentDecorator Decorator) Create()
    {
        var caller = new Mock<IUserAuthCatalogiServiceAgent>();
        var cached = new Mock<ICatalogiServiceAgent>();

        return (caller, cached, new CatalogiServiceAgentDecorator(caller.Object, cached.Object));
    }

    [Fact]
    public async Task FirstCall_IsMadeAsTheCaller_NotThroughTheCachedAgent()
    {
        var (caller, cached, decorator) = Create();
        caller.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null)).ReturnsAsync(Ok(new ZaakTypeResponseDto()));

        await decorator.GetZaakTypeByUrlAsync(ZaakTypeUrl1);

        caller.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null), Times.Once);
        cached.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CallsAfterTheCallerWasAccepted_GoThroughTheCachedAgent_OfAnyType()
    {
        var (caller, cached, decorator) = Create();
        caller.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null)).ReturnsAsync(Ok(new ZaakTypeResponseDto()));
        cached.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl2, "catalogus")).ReturnsAsync(Ok(new ZaakTypeResponseDto()));
        cached.Setup(a => a.GetRolTypeByUrlAsync(RolTypeUrl)).ReturnsAsync(Ok(new RolTypeResponseDto()));

        await decorator.GetZaakTypeByUrlAsync(ZaakTypeUrl1);
        await decorator.GetZaakTypeByUrlAsync(ZaakTypeUrl2, "catalogus");
        await decorator.GetRolTypeByUrlAsync(RolTypeUrl);

        caller.Verify(a => a.GetZaakTypeByUrlAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        caller.VerifyNoOtherCalls();
        cached.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl2, "catalogus"), Times.Once);
        cached.Verify(a => a.GetRolTypeByUrlAsync(RolTypeUrl), Times.Once);
    }

    [Fact]
    public async Task FirstCallRefused_ThrowsWithTheStatus_AndTheNextCallIsStillMadeAsTheCaller()
    {
        var (caller, cached, decorator) = Create();
        caller.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null)).ReturnsAsync(Failed<ZaakTypeResponseDto>(403));
        caller.Setup(a => a.GetRolTypeByUrlAsync(RolTypeUrl)).ReturnsAsync(Failed<RolTypeResponseDto>(403));

        var first = await Assert.ThrowsAsync<ExpandExternalServiceException>(() => decorator.GetZaakTypeByUrlAsync(ZaakTypeUrl1));
        await Assert.ThrowsAsync<ExpandExternalServiceException>(() => decorator.GetRolTypeByUrlAsync(RolTypeUrl));

        Assert.Equal(403, first.StatusCode);
        Assert.Equal(ZaakTypeUrl1, first.ServiceUrl);
        caller.Verify(a => a.GetRolTypeByUrlAsync(RolTypeUrl), Times.Once);
        // Note: a caller that was refused is never served from the shared cache
        cached.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CallerAccepted_ButTheCachedAgentFails_ThrowsWithTheStatus()
    {
        var (caller, cached, decorator) = Create();
        caller.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null)).ReturnsAsync(Ok(new ZaakTypeResponseDto()));
        cached.Setup(a => a.GetRolTypeByUrlAsync(RolTypeUrl)).ReturnsAsync(Failed<RolTypeResponseDto>(404));

        await decorator.GetZaakTypeByUrlAsync(ZaakTypeUrl1);
        var exception = await Assert.ThrowsAsync<ExpandExternalServiceException>(() => decorator.GetRolTypeByUrlAsync(RolTypeUrl));

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal(RolTypeUrl, exception.ServiceUrl);
    }

    [Fact]
    public async Task EveryRequestHasItsOwnDecorator_AndSoItsOwnCheckOfTheCaller()
    {
        var caller = new Mock<IUserAuthCatalogiServiceAgent>();
        var cached = new Mock<ICatalogiServiceAgent>();
        caller.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null)).ReturnsAsync(Ok(new ZaakTypeResponseDto()));

        await new CatalogiServiceAgentDecorator(caller.Object, cached.Object).GetZaakTypeByUrlAsync(ZaakTypeUrl1);
        await new CatalogiServiceAgentDecorator(caller.Object, cached.Object).GetZaakTypeByUrlAsync(ZaakTypeUrl1);

        caller.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null), Times.Exactly(2));
        cached.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AnExceptionOfTheAgent_IsWrappedAndKept()
    {
        var (caller, _, decorator) = Create();
        var cause = new System.TimeoutException("timeout");
        caller.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl1, null)).ThrowsAsync(cause);

        var exception = await Assert.ThrowsAsync<ExpandExternalServiceException>(() => decorator.GetZaakTypeByUrlAsync(ZaakTypeUrl1));

        Assert.Same(cause, exception.InnerException);
        Assert.Null(exception.StatusCode);
    }
}
