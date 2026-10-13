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
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakRollenResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    private static readonly ZaakResponseDto EntityWithTwoRollen = new()
    {
        Url = ZaakUrl,
        Rollen = ["https://zrc.test/rollen/1", "https://zrc.test/rollen/2"],
    };

    // ZaakRollenResolver resolves IMediator from a fresh DI scope per call (see the class doc comment:
    // it must not reuse the request-scoped DbContext/connection, since GetAllZaakRolQuery's handler can
    // create a session-scoped PostgreSQL temp table). A real ServiceCollection exercises that
    // CreateScope() call for real, unlike a bare IMediator mock would.
    private static IServiceProvider BuildServiceProvider(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ResolveAsync_ZaakHasRollen_SendsGetAllZaakRolQueryFilteredByZaakAndSizedToRolCountAndReturnsMappedList()
    {
        var zaakRol1 = new ZaakRol { Id = new("00000000-0000-0000-0000-000000000001") };
        var zaakRol2 = new ZaakRol { Id = new("00000000-0000-0000-0000-000000000002") };
        var pagedResult = new PagedResult<ZaakRol> { PageResult = [zaakRol1, zaakRol2], Count = 2 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(
                    It.Is<GetAllZaakRolQuery>(q => q.GetAllZaakRolFilter.Zaak == ZaakUrl && q.Pagination.Page == 1 && q.Pagination.Size == 2),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new QueryResult<PagedResult<ZaakRol>>(pagedResult, QueryStatus.OK));
        var mappedRol1 = new RolResponseDto { Uuid = zaakRol1.Id.ToString() };
        var mappedRol2 = new RolResponseDto { Uuid = zaakRol2.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<RolResponseDto>>(pagedResult.PageResult)).Returns([mappedRol1, mappedRol2]);
        var rolExpandEngine = new ExpandEngine<RolResponseDto>([]);

        var resolver = new ZaakRollenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object, rolExpandEngine);

        var result = await resolver.ResolveAsync(EntityWithTwoRollen, new Dictionary<string, object>(), new HashSet<string> { "rollen" });

        var rollen = Assert.IsType<List<RolResponseDto>>(result);
        Assert.Equal([mappedRol1, mappedRol2], rollen);
    }

    [Fact]
    public async Task ResolveAsync_NoRollen_ReturnsEmptyListWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakRollenResolver(BuildServiceProvider(mediatorMock.Object), Mock.Of<IMapper>(), new ExpandEngine<RolResponseDto>([]));
        var entity = new ZaakResponseDto { Url = ZaakUrl, Rollen = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "rollen" });

        var rollen = Assert.IsType<List<RolResponseDto>>(result);
        Assert.Empty(rollen);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetAllZaakRolQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_RollenRoltypeRequested_DelegatesToRolExpandEngineResolveListAsync()
    {
        var zaakRol1 = new ZaakRol { Id = new("00000000-0000-0000-0000-000000000001") };
        var pagedResult = new PagedResult<ZaakRol> { PageResult = [zaakRol1], Count = 1 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakRolQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<PagedResult<ZaakRol>>(pagedResult, QueryStatus.OK));
        var mappedRol1 = new RolResponseDto { Uuid = zaakRol1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<RolResponseDto>>(pagedResult.PageResult)).Returns([mappedRol1]);

        var roltypeResolverMock = new Mock<IExpandResolver<RolResponseDto>>();
        roltypeResolverMock.SetupGet(r => r.Path).Returns("roltype");
        roltypeResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var rolExpandEngine = new ExpandEngine<RolResponseDto>([roltypeResolverMock.Object]);

        var entityWithOneRol = new ZaakResponseDto { Url = ZaakUrl, Rollen = ["https://zrc.test/rollen/1"] };
        var resolver = new ZaakRollenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object, rolExpandEngine);

        await resolver.ResolveAsync(entityWithOneRol, new Dictionary<string, object>(), new HashSet<string> { "rollen", "rollen.roltype" });

        roltypeResolverMock.Verify(
            r => r.ResolveAsync(mappedRol1, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsRollen_AndHasNoParent_AndDeclaresRollenRoltypeAsAdditionalPath()
    {
        var resolver = new ZaakRollenResolver(BuildServiceProvider(Mock.Of<IMediator>()), Mock.Of<IMapper>(), new ExpandEngine<RolResponseDto>([]));

        Assert.Equal("rollen", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal([("rollen.roltype", "rollen")], resolver.AdditionalPaths);
    }
}
