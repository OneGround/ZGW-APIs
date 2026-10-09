using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The informatieobjecttype cache is shared by all expand paths of one request, so a type fetched without its catalogus must not be
/// served to a later path that asks for the catalogus (and the other way around).
/// </summary>
public class InformatieObjectTypeResolverCacheKeyTests
{
    private const string TypeUrl = "http://catalogi.local/api/v1/informatieobjecttypen/11111111-1111-1111-1111-111111111111";

    private static (
        Mock<ICatalogiServiceAgentDecorator> Agent,
        InformatieObjectTypeResponseDto WithoutCatalogus,
        InformatieObjectTypeResponseDto WithCatalogus
    ) SetUpAgent()
    {
        var withoutCatalogus = new InformatieObjectTypeResponseDto { Url = TypeUrl };
        var withCatalogus = new InformatieObjectTypeResponseDto { Url = TypeUrl };

        var agent = new Mock<ICatalogiServiceAgentDecorator>();
        agent
            .Setup(a => a.GetInformatieObjectTypeByUrlAsync(TypeUrl, null))
            .ReturnsAsync(new ServiceAgentResponse<InformatieObjectTypeResponseDto>(withoutCatalogus));
        agent
            .Setup(a => a.GetInformatieObjectTypeByUrlAsync(TypeUrl, "catalogus"))
            .ReturnsAsync(new ServiceAgentResponse<InformatieObjectTypeResponseDto>(withCatalogus));

        return (agent, withoutCatalogus, withCatalogus);
    }

    [Fact]
    public async Task EnkelvoudigInformatieObjectResolver_SameTypeWithAndWithoutCatalogus_AreNotMixedUp()
    {
        var (agent, withoutCatalogus, withCatalogus) = SetUpAgent();
        var resolver = new EnkelvoudigInformatieObject_InformatieObjectType_Resolver(
            agent.Object,
            new GenericCache<InformatieObjectTypeResponseDto>()
        );
        var entity = new EnkelvoudigInformatieObjectGetResponseDto { InformatieObjectType = TypeUrl };

        var first = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "informatieobjecttype" });
        var second = await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobjecttype", "informatieobjecttype.catalogus" }
        );
        var secondAgain = await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobjecttype", "informatieobjecttype.catalogus" }
        );

        Assert.Same(withoutCatalogus, first);
        Assert.Same(withCatalogus, second);
        Assert.Same(withCatalogus, secondAgain);
        agent.Verify(a => a.GetInformatieObjectTypeByUrlAsync(TypeUrl, It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task NestedResolver_SameTypeWithAndWithoutCatalogus_AreNotMixedUp()
    {
        var (agent, withoutCatalogus, withCatalogus) = SetUpAgent();
        var resolver = new InformatieObjectTypeResolver<object>(agent.Object, new GenericCache<InformatieObjectTypeResponseDto>());
        var resolved = new Dictionary<string, object>
        {
            ["informatieobject"] = new EnkelvoudigInformatieObjectGetResponseDto { InformatieObjectType = TypeUrl },
        };

        var first = await resolver.ResolveAsync(new object(), resolved, new HashSet<string> { resolver.Path });
        var second = await resolver.ResolveAsync(new object(), resolved, new HashSet<string> { resolver.Path, $"{resolver.Path}.catalogus" });

        Assert.Same(withoutCatalogus, first);
        Assert.Same(withCatalogus, second);
        agent.Verify(a => a.GetInformatieObjectTypeByUrlAsync(TypeUrl, It.IsAny<string>()), Times.Exactly(2));
    }
}
