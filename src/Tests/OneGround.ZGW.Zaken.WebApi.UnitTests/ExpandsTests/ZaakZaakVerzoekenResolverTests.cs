using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;
using ZaakResponseDto = OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakZaakVerzoekenResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    // Note: the resolver opens a fresh DI scope per call (see its doc comment), so a real ServiceCollection is used, not a bare IMediator
    private static IServiceProvider BuildServiceProvider(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        return services.BuildServiceProvider();
    }

    private static Mock<IMediator> MediatorReturning(QueryResult<IList<ZaakVerzoek>> result)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetAllZaakVerzoekenQuery>(q => q.GetAllZaakVerzoekenFilter.Zaak == ZaakUrl), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mediatorMock;
    }

    [Fact]
    public async Task ResolveAsync_SendsGetAllZaakVerzoekenQueryFilteredByZaakAndReturnsMappedList()
    {
        IList<ZaakVerzoek> zaakverzoeken = [new ZaakVerzoek { Id = new("00000000-0000-0000-0000-000000000001") }];
        var mediatorMock = MediatorReturning(new QueryResult<IList<ZaakVerzoek>>(zaakverzoeken, QueryStatus.OK));
        var mapped = new ZaakVerzoekResponseDto { Uuid = "00000000-0000-0000-0000-000000000001" };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakVerzoekResponseDto>>(zaakverzoeken)).Returns([mapped]);

        var resolver = new ZaakZaakVerzoekenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object);

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto { Url = ZaakUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaakverzoeken" }
        );

        Assert.Equal([mapped], Assert.IsType<List<ZaakVerzoekResponseDto>>(result));
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_ListNotAvailableToTheCaller_ResolvesToAnEmptyList(QueryStatus status)
    {
        var mediatorMock = MediatorReturning(new QueryResult<IList<ZaakVerzoek>>(null, status));
        var resolver = new ZaakZaakVerzoekenResolver(BuildServiceProvider(mediatorMock.Object), Mock.Of<IMapper>());

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto { Url = ZaakUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaakverzoeken" }
        );

        Assert.Empty(Assert.IsType<List<ZaakVerzoekResponseDto>>(result));
    }

    [Fact]
    public async Task ResolveAsync_OtherFailureStatus_Throws()
    {
        var mediatorMock = MediatorReturning(new QueryResult<IList<ZaakVerzoek>>(null, QueryStatus.ValidationError));
        var resolver = new ZaakZaakVerzoekenResolver(BuildServiceProvider(mediatorMock.Object), Mock.Of<IMapper>());

        await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(new ZaakResponseDto { Url = ZaakUrl }, new Dictionary<string, object>(), new HashSet<string> { "zaakverzoeken" })
        );
    }

    [Fact]
    public void Path_IsZaakverzoeken_AndHasNoParent()
    {
        var resolver = new ZaakZaakVerzoekenResolver(BuildServiceProvider(Mock.Of<IMediator>()), Mock.Of<IMapper>());

        Assert.Equal("zaakverzoeken", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
