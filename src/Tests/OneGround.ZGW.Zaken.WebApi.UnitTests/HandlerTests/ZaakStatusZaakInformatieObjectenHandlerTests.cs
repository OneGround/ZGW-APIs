using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Handlers;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using OneGround.ZGW.Zaken.Web.Services;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

// Note: the zaakinformatieobjecten of a status are only the ones that belong to that status, not all of the zaak. Get and GetAll must agree.
public class ZaakStatusZaakInformatieObjectenHandlerTests : IAsyncLifetime
{
    private const string TestOwner = "999993653";
    private const string TestZaakTypeUrl = "http://catalogi.local/api/v1/zaaktypen/33333333-3333-3333-3333-333333333333";

    private static readonly Guid ZaakId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Status1Id = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Status2Id = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid ZioOfStatus1Id = new("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid ZioOfStatus2Id = new("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid ZioWithoutStatusId = new("bbbbbbbb-0000-0000-0000-000000000003");
    private static readonly Guid OtherZaakId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ZioOfOtherZaakWithStatus1Id = new("bbbbbbbb-0000-0000-0000-000000000004");

    private ZrcDbContext _dbContext;
    private Mock<IEntityUriService> _uriServiceMock;
    private Mock<IAuthorizationContextAccessor> _authorizationContextAccessorMock;
    private Mock<IZaakKenmerkenResolver> _zaakKenmerkenResolverMock;
    private IConfiguration _configuration;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase(databaseName: $"zrc-{Guid.NewGuid()}").Options;

        _dbContext = new UnitTestZrcDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _dbContext.Zaken.AddRange(NewZaak(ZaakId, "ZAAK-2024-001"), NewZaak(OtherZaakId, "ZAAK-2024-002"));
        _dbContext.ZaakStatussen.AddRange(NewStatus(Status1Id, DateTime.UtcNow.AddMinutes(-2)), NewStatus(Status2Id, DateTime.UtcNow.AddMinutes(-1)));
        _dbContext.ZaakInformatieObjecten.AddRange(
            NewZaakInformatieObject(ZioOfStatus1Id, Status1Id),
            NewZaakInformatieObject(ZioOfStatus2Id, Status2Id),
            NewZaakInformatieObject(ZioWithoutStatusId, null),
            // Note: the validators do not enforce that the status of a zaakinformatieobject belongs to the same zaak; such a mismatch is left out
            NewZaakInformatieObject(ZioOfOtherZaakWithStatus1Id, Status1Id, OtherZaakId)
        );
        await _dbContext.SaveChangesAsync();

        _uriServiceMock = new Mock<IEntityUriService>();
        _zaakKenmerkenResolverMock = new Mock<IZaakKenmerkenResolver>();

        // Authorization: allow everything
        _authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        _authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        // Configuration: Application section is required by ZakenBaseHandler
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" }, { "Application:SkipMigrationsAtStartup", "true" } }
            )
            .Build();
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    private static Zaak NewZaak(Guid id, string identificatie) =>
        new()
        {
            Id = id,
            Owner = TestOwner,
            Zaaktype = TestZaakTypeUrl,
            Bronorganisatie = TestOwner,
            Identificatie = identificatie,
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            Communicatiekanaal = string.Empty,
            Selectielijstklasse = string.Empty,
            VerantwoordelijkeOrganisatie = TestOwner,
        };

    private static ZaakStatus NewStatus(Guid id, DateTime creationTime) =>
        new()
        {
            Id = id,
            Owner = TestOwner,
            ZaakId = ZaakId,
            StatusType = "http://catalogi.local/api/v1/statustypen/44444444-4444-4444-4444-444444444444",
            DatumStatusGezet = creationTime,
            CreationTime = creationTime,
        };

    private static ZaakInformatieObject NewZaakInformatieObject(Guid id, Guid? statusId, Guid? zaakId = null) =>
        new()
        {
            Id = id,
            Owner = TestOwner,
            ZaakId = zaakId ?? ZaakId,
            StatusId = statusId,
            InformatieObject = $"http://documenten.local/api/v1/enkelvoudiginformatieobjecten/{id}",
            CreationTime = DateTime.UtcNow,
        };

    [Fact]
    public async Task GetZaakStatus_ReturnsOnlyTheZaakInformatieObjectenOfThatStatus()
    {
        var sut = new GetZaakStatusQueryHandler(
            NullLogger<GetZaakStatusQueryHandler>.Instance,
            _configuration,
            _uriServiceMock.Object,
            _dbContext,
            _authorizationContextAccessorMock.Object,
            _zaakKenmerkenResolverMock.Object
        );

        var result = await sut.Handle(new GetZaakStatusQuery { Id = Status1Id }, CancellationToken.None);

        Assert.Equal(QueryStatus.OK, result.Status);
        Assert.Equal([ZioOfStatus1Id], result.Result.Zaak.ZaakInformatieObjecten.Select(z => z.Id));
    }

    private GetAllZaakStatussenQueryHandler CreateGetAllSut() =>
        new(
            NullLogger<GetAllZaakStatussenQueryHandler>.Instance,
            _configuration,
            _dbContext,
            _uriServiceMock.Object,
            _authorizationContextAccessorMock.Object,
            Mock.Of<IZaakAuthorizationTempTableService>(),
            _zaakKenmerkenResolverMock.Object
        );

    private static GetAllZaakStatussenQuery NewGetAllQuery(int page) =>
        new()
        {
            GetAllZaakStatussenFilter = new GetAllZaakStatussenFilter(),
            Pagination = new PaginationFilter { Page = page, Size = 10 },
        };

    [Fact]
    public async Task GetAllZaakStatussen_EmptyPage_ReturnsNoStatussen()
    {
        var result = await CreateGetAllSut().Handle(NewGetAllQuery(page: 2), CancellationToken.None);

        Assert.Equal(QueryStatus.OK, result.Status);
        Assert.Empty(result.Result.PageResult);
        Assert.Equal(2, result.Result.Count);
    }

    [Fact]
    public async Task GetAllZaakStatussen_EveryStatusGetsOnlyItsOwnZaakInformatieObjecten()
    {
        var result = await CreateGetAllSut().Handle(NewGetAllQuery(page: 1), CancellationToken.None);

        Assert.Equal(QueryStatus.OK, result.Status);
        var statussen = result.Result.PageResult.ToDictionary(s => s.Id);
        Assert.Equal(2, statussen.Count);
        Assert.Equal([ZioOfStatus1Id], statussen[Status1Id].Zaak.ZaakInformatieObjecten.Select(z => z.Id));
        Assert.Equal([ZioOfStatus2Id], statussen[Status2Id].Zaak.ZaakInformatieObjecten.Select(z => z.Id));
    }
}
