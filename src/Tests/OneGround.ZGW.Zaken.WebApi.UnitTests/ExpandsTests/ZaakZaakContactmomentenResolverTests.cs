using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakZaakContactmomentenResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    // ZaakZaakContactmomentenResolver resolves IMediator from a fresh DI scope per call (see the class
    // doc comment: it must not reuse the request-scoped DbContext/connection, since
    // GetAllZaakContactmomentenQuery's handler can create a session-scoped PostgreSQL temp table). A
    // real ServiceCollection exercises that CreateScope() call for real, unlike a bare IMediator mock
    // would.
    private static IServiceProvider BuildServiceProvider(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        return services.BuildServiceProvider();
    }

    // Unlike ZaakRollenResolver/ZaakZaakObjectenResolver, there is no "Rollen"/"ZaakObjecten"-style
    // count field on ZaakResponseDto to short-circuit on (ZAAK has no navigation back to its
    // ZAAKCONTACTMOMENTen at all), so this resolver always queries -- exactly like the old (v1._5)
    // ZaakContactmomentenExpander already does.
    [Fact]
    public async Task ResolveAsync_AlwaysSendsGetAllZaakContactmomentenQueryFilteredByZaakAndReturnsMappedList()
    {
        var zaakContactmoment1 = new ZaakContactmoment { Id = new("00000000-0000-0000-0000-000000000001") };
        var zaakContactmoment2 = new ZaakContactmoment { Id = new("00000000-0000-0000-0000-000000000002") };
        IList<ZaakContactmoment> zaakcontactmomenten = [zaakContactmoment1, zaakContactmoment2];
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(It.Is<GetAllZaakContactmomentenQuery>(q => q.GetAllZaakContactmomentenFilter.Zaak == ZaakUrl), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new QueryResult<IList<ZaakContactmoment>>(zaakcontactmomenten, QueryStatus.OK));
        var mappedZaakContactmoment1 = new ZaakContactmomentResponseDto { Uuid = zaakContactmoment1.Id.ToString() };
        var mappedZaakContactmoment2 = new ZaakContactmomentResponseDto { Uuid = zaakContactmoment2.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<List<ZaakContactmomentResponseDto>>(zaakcontactmomenten))
            .Returns([mappedZaakContactmoment1, mappedZaakContactmoment2]);

        var resolver = new ZaakZaakContactmomentenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object);
        var entity = new ZaakResponseDto { Url = ZaakUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaakcontactmomenten" });

        var zaakcontactmomentenResult = Assert.IsType<List<ZaakContactmomentResponseDto>>(result);
        Assert.Equal([mappedZaakContactmoment1, mappedZaakContactmoment2], zaakcontactmomentenResult);
    }

    [Fact]
    public void Path_IsZaakcontactmomenten_AndHasNoParent()
    {
        var resolver = new ZaakZaakContactmomentenResolver(BuildServiceProvider(Mock.Of<IMediator>()), Mock.Of<IMapper>());

        Assert.Equal("zaakcontactmomenten", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
