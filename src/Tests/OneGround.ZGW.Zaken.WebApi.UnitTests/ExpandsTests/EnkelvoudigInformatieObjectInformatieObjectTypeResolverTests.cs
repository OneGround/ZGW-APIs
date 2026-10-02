using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class EnkelvoudigInformatieObjectInformatieObjectTypeResolverTests
{
    private const string InformatieObjectTypeUrl = "http://catalogi.local/api/v1/informatieobjecttypen/11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task ResolveAsync_InformatieobjecttypeRequested_CallsServiceAgentAndReturnsInformatieobjecttype()
    {
        var informatieobjecttype = new InformatieObjectTypeResponseDto { Url = InformatieObjectTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetInformatieObjectTypeByUrlAsync(InformatieObjectTypeUrl, null))
            .ReturnsAsync(new ServiceAgentResponse<InformatieObjectTypeResponseDto>(informatieobjecttype));

        var resolver = new EnkelvoudigInformatieObjectInformatieObjectTypeResolver(
            serviceAgentMock.Object,
            new GenericCache<InformatieObjectTypeResponseDto>()
        );
        var entity = new EnkelvoudigInformatieObjectGetResponseDto { InformatieObjectType = InformatieObjectTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "informatieobjecttype" });

        Assert.Same(informatieobjecttype, result);
        serviceAgentMock.Verify(a => a.GetInformatieObjectTypeByUrlAsync(InformatieObjectTypeUrl, null), Times.Once);
    }

    [Fact]
    public void Path_IsInformatieobjecttype_AndHasNoParent()
    {
        var resolver = new EnkelvoudigInformatieObjectInformatieObjectTypeResolver(
            Mock.Of<ICatalogiServiceAgentDecorator>(),
            new GenericCache<InformatieObjectTypeResponseDto>()
        );

        Assert.Equal("informatieobjecttype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
