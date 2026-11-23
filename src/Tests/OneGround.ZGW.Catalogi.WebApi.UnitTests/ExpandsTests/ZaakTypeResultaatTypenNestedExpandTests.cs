using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>The paths below "resultaattypen" of a ZaakType: for each ResultaatType the paths of its own engine are resolved.</summary>
public class ZaakTypeResultaatTypenNestedExpandTests
{
    private const string ResultaatTypeUrl1 = "https://ztc.test/resultaattypen/11111111-1111-1111-1111-111111111111";
    private const string ResultaatTypeUrl2 = "https://ztc.test/resultaattypen/22222222-2222-2222-2222-222222222222";
    private const string Klasse1 = "https://selectielijst.test/klasse/1";
    private const string Klasse2 = "https://selectielijst.test/klasse/2";
    private const string Omschrijving1 = "https://selectielijst.test/omschrijving/1";

    private readonly Mock<IExternalJsonClient> _externalJsonClient = new();
    private readonly ExpandValidator<V4.ZaakTypeResponseDto> _validator;
    private readonly ExpandEngine<V4.ZaakTypeResponseDto> _engine;

    public ZaakTypeResultaatTypenNestedExpandTests()
    {
        _externalJsonClient
            .Setup(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string url, CancellationToken _) => new JObject { ["url"] = url });

        var dtos = new Dictionary<Guid, V4.ResultaatTypeResponseDto>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new() { SelectieLijstKlasse = Klasse1, ResultaatTypeOmschrijving = Omschrijving1 },
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new() { SelectieLijstKlasse = Klasse2 },
        };

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetResultaatTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (GetResultaatTypeQuery q, CancellationToken _) => new QueryResult<ResultaatType>(new ResultaatType { Id = q.Id }, QueryStatus.OK)
            );
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<V4.ResultaatTypeResponseDto>(It.IsAny<ResultaatType>())).Returns((ResultaatType r) => dtos[r.Id]);

        var resultaatTypeEngine = new ExpandEngine<V4.ResultaatTypeResponseDto>([
            new ResultaatType_SelectielijstKlasse_Resolver(_externalJsonClient.Object),
            new ResultaatType_ResultaatTypeOmschrijving_Resolver(_externalJsonClient.Object),
        ]);
        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapperMock.Object);
        services.AddScoped(typeof(IGenericCache<>), typeof(GenericCache<>));
        services.AddScoped(_ => resultaatTypeEngine);
        var provider = services.BuildServiceProvider();

        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(u => u.GetId(It.IsAny<string>())).Returns((string url) => Guid.Parse(url[^36..]));

        IExpandResolver<V4.ZaakTypeResponseDto>[] resolvers = [new ZaakType_ResultaatTypen_Resolver(provider, uriService.Object)];
        _validator = new ExpandValidator<V4.ZaakTypeResponseDto>(resolvers);
        _engine = new ExpandEngine<V4.ZaakTypeResponseDto>(resolvers);
    }

    private static V4.ZaakTypeResponseDto ZaakType(params string[] resultaatTypen) => new() { ResultaatTypen = resultaatTypen };

    [Theory]
    [InlineData("resultaattypen.selectielijstklasse")]
    [InlineData("resultaattypen.resultaattypeomschrijving")]
    public void Validator_AcceptsAPathBelowResultaattypen_AndImpliesResultaattypen(string path)
    {
        var (paths, error) = _validator.ParseAndValidate(path);

        Assert.Null(error);
        Assert.Contains("resultaattypen", paths);
        Assert.Contains(path, paths);
    }

    [Fact]
    public async Task ResolveAsync_SelectielijstklasseRequested_EmbedsItInEachResultaatType()
    {
        var zaakType = ZaakType(ResultaatTypeUrl1, ResultaatTypeUrl2);

        await _engine.ResolveAsync(zaakType, ["resultaattypen", "resultaattypen.selectielijstklasse"]);

        var resultaatTypen = Assert.IsType<List<V4.ResultaatTypeResponseDto>>(zaakType.Expand["resultaattypen"]);
        Assert.Equal(Klasse1, ((JObject)resultaatTypen[0].Expand["selectielijstklasse"])["url"]!.Value<string>());
        Assert.Equal(Klasse2, ((JObject)resultaatTypen[1].Expand["selectielijstklasse"])["url"]!.Value<string>());
        Assert.DoesNotContain("resultaattypeomschrijving", resultaatTypen[0].Expand.Keys);
    }

    [Fact]
    public async Task ResolveAsync_BothChildPathsRequested_EmbedsBoth()
    {
        var zaakType = ZaakType(ResultaatTypeUrl1);

        await _engine.ResolveAsync(zaakType, ["resultaattypen", "resultaattypen.selectielijstklasse", "resultaattypen.resultaattypeomschrijving"]);

        var resultaatType = ((List<V4.ResultaatTypeResponseDto>)zaakType.Expand["resultaattypen"]).Single();
        Assert.Equal(["resultaattypeomschrijving", "selectielijstklasse"], resultaatType.Expand.Keys.Order());
    }

    [Fact]
    public async Task ResolveAsync_OnlyResultaattypenRequested_EmbedsNothingBelowIt()
    {
        var zaakType = ZaakType(ResultaatTypeUrl1);

        await _engine.ResolveAsync(zaakType, ["resultaattypen"]);

        Assert.Null(((List<V4.ResultaatTypeResponseDto>)zaakType.Expand["resultaattypen"]).Single().Expand);
        _externalJsonClient.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveListAsync_ZaakTypenSharingAResultaatType_FetchTheExternalDocumentOnce()
    {
        var zaakTypen = new[] { ZaakType(ResultaatTypeUrl1), ZaakType(ResultaatTypeUrl1, ResultaatTypeUrl2) };

        await _engine.ResolveListAsync(zaakTypen, ["resultaattypen", "resultaattypen.selectielijstklasse"]);

        // Note: the cache of the request makes it one ResultaatType, and one that is already expanded is not expanded again
        _externalJsonClient.Verify(c => c.GetJsonObjectAsync(Klasse1, It.IsAny<CancellationToken>()), Times.Once);
        _externalJsonClient.Verify(c => c.GetJsonObjectAsync(Klasse2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
