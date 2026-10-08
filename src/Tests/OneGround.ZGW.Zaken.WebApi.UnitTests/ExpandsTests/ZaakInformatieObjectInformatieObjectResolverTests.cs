using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakInformatieObjectInformatieObjectResolverTests
{
    private const string InformatieObjectUrl = "https://drc.test/enkelvoudiginformatieobjecten/66666666-6666-6666-6666-666666666666";

    [Fact]
    public async Task ResolveAsync_NoDeeperExpandRequested_ReturnsMappedInformatieobjectWithoutResolvingInformatieobjecttype()
    {
        var informatieobject = new EnkelvoudigInformatieObjectResponseDto { Url = InformatieObjectUrl };
        var serviceAgentMock = new Mock<IUserAuthDocumentenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(InformatieObjectUrl))
            .ReturnsAsync(new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(informatieobject));

        var mappedInformatieobject = new EnkelvoudigInformatieObjectGetResponseDto { Url = InformatieObjectUrl };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieobject)).Returns(mappedInformatieobject);

        var informatieobjecttypeResolverMock = new Mock<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>();
        informatieobjecttypeResolverMock.SetupGet(r => r.Path).Returns("informatieobjecttype");
        informatieobjecttypeResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([informatieobjecttypeResolverMock.Object]);

        var resolver = new ZaakInformatieObjectInformatieObjectResolver(serviceAgentMock.Object, mapperMock.Object, expandEngine);
        var entity = new ZaakInformatieObjectResponseDto { InformatieObject = InformatieObjectUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "informatieobject" });

        Assert.Same(mappedInformatieobject, result);
        informatieobjecttypeResolverMock.Verify(
            r =>
                r.ResolveAsync(
                    It.IsAny<EnkelvoudigInformatieObjectGetResponseDto>(),
                    It.IsAny<IReadOnlyDictionary<string, object>>(),
                    It.IsAny<IReadOnlySet<string>>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task ResolveAsync_InformatieobjecttypeRequested_ResolvesItViaExpandEngine()
    {
        var informatieobject = new EnkelvoudigInformatieObjectResponseDto { Url = InformatieObjectUrl };
        var serviceAgentMock = new Mock<IUserAuthDocumentenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(InformatieObjectUrl))
            .ReturnsAsync(new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(informatieobject));

        var mappedInformatieobject = new EnkelvoudigInformatieObjectGetResponseDto { Url = InformatieObjectUrl };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieobject)).Returns(mappedInformatieobject);

        var informatieobjecttypeResolverMock = new Mock<IExpandResolver<EnkelvoudigInformatieObjectGetResponseDto>>();
        informatieobjecttypeResolverMock.SetupGet(r => r.Path).Returns("informatieobjecttype");
        informatieobjecttypeResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([informatieobjecttypeResolverMock.Object]);

        var resolver = new ZaakInformatieObjectInformatieObjectResolver(serviceAgentMock.Object, mapperMock.Object, expandEngine);
        var entity = new ZaakInformatieObjectResponseDto { InformatieObject = InformatieObjectUrl };

        var result = await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobject", "informatieobject.informatieobjecttype" }
        );

        Assert.Same(mappedInformatieobject, result);
        informatieobjecttypeResolverMock.Verify(
            r =>
                r.ResolveAsync(
                    mappedInformatieobject,
                    It.IsAny<IReadOnlyDictionary<string, object>>(),
                    It.Is<IReadOnlySet<string>>(p => p.Contains("informatieobjecttype"))
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ResolveAsync_DrcCallFails_ThrowsExpandExternalServiceException()
    {
        var serviceAgentMock = new Mock<IUserAuthDocumentenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(InformatieObjectUrl))
            .ReturnsAsync(new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(new ErrorResponse { Status = 502 }, null));

        var resolver = new ZaakInformatieObjectInformatieObjectResolver(
            serviceAgentMock.Object,
            Mock.Of<IMapper>(),
            new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([])
        );
        var entity = new ZaakInformatieObjectResponseDto { InformatieObject = InformatieObjectUrl };

        var ex = await Assert.ThrowsAsync<ExpandExternalServiceException>(() =>
            resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "informatieobject" })
        );

        Assert.Equal("DRC", ex.ServiceName);
        Assert.Equal(InformatieObjectUrl, ex.ServiceUrl);
        Assert.Equal(502, ex.StatusCode);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(404)]
    public async Task ResolveAsync_DocumentNotAvailableToTheCaller_ResolvesToNullInsteadOfFailingTheRequest(int status)
    {
        var serviceAgentMock = new Mock<IUserAuthDocumentenServiceAgent>();
        serviceAgentMock
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(InformatieObjectUrl))
            .ReturnsAsync(new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(new ErrorResponse { Status = status }, null));

        var resolver = new ZaakInformatieObjectInformatieObjectResolver(
            serviceAgentMock.Object,
            Mock.Of<IMapper>(),
            new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([])
        );

        var result = await resolver.ResolveAsync(
            new ZaakInformatieObjectResponseDto { InformatieObject = InformatieObjectUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobject" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_IsInformatieobject_AndHasNoParent_AndDeclaresInformatieobjecttypeAdditionalPaths()
    {
        var resolver = new ZaakInformatieObjectInformatieObjectResolver(
            Mock.Of<IUserAuthDocumentenServiceAgent>(),
            Mock.Of<IMapper>(),
            new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([])
        );

        Assert.Equal("informatieobject", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal([("informatieobject.informatieobjecttype", "informatieobject")], resolver.AdditionalPaths);
    }
}
