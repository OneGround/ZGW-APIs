using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.BusinessRules;
using OneGround.ZGW.Besluiten.Web.Handlers;
using OneGround.ZGW.Besluiten.Web.Handlers.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.HandlerTests;

public class BesluitInformatieObjectAuthorizationTests : IDisposable
{
    // The handlers compare owners as plain strings, so this is deliberately not an RSIN.
    private const string CurrentTenant = "tenant-current";
    private const string BesluitTypeA = "http://catalogi.local/api/v1/besluittypen/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string BesluitTypeB = "http://catalogi.local/api/v1/besluittypen/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private const string BesluitUrl = "http://besluiten.local/api/v1/besluiten/cccccccc-cccc-cccc-cccc-cccccccccccc";

    private readonly BrcDbContext _context;
    private readonly Mock<IEntityUriService> _uriService = new();
    private readonly Mock<IAuditTrailFactory> _auditTrailFactory = new();
    private readonly Mock<IBesluitInformatieObjectBusinessRuleService> _businessRules = new();
    private readonly Mock<IBesluitKenmerkenResolver> _kenmerkenResolver = new();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(
            new Dictionary<string, string>
            {
                { "Application:DontSendNotificaties", "true" },
                { "Application:SkipMigrationsAtStartup", "true" },
                { "Application:IgnoreInformatieObjectValidation", "true" },
            }
        )
        .Build();

    public BesluitInformatieObjectAuthorizationTests()
    {
        var options = new DbContextOptionsBuilder<BrcDbContext>().UseInMemoryDatabase($"brc-{Guid.NewGuid()}").Options;
        _context = new BrcDbContext(options);
        _context.Database.EnsureCreated();

        _auditTrailFactory.Setup(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(new Mock<IAuditTrailService>().Object);
        _businessRules
            .Setup(r =>
                r.ValidateAsync(It.IsAny<Besluit>(), It.IsAny<BesluitInformatieObject>(), It.IsAny<bool>(), It.IsAny<List<ValidationError>>())
            )
            .ReturnsAsync(true);
        // Notification building mutates the kenmerken dictionary, so it must never be null.
        _kenmerkenResolver
            .Setup(r => r.GetKenmerkenAsync(It.IsAny<Besluit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Dictionary<string, string>());
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Reading_a_relation_on_a_besluittype_the_client_is_not_authorized_for_is_forbidden()
    {
        var id = await SeedBesluitInformatieObject(BesluitTypeA);

        var result = await GetHandler(Client(AuthorizationScopes.Besluiten.Read, BesluitTypeB))
            .Handle(new GetBesluitInformatieObjectQuery { Id = id }, CancellationToken.None);

        Assert.Equal(QueryStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Reading_a_relation_on_an_authorized_besluittype_returns_it()
    {
        var id = await SeedBesluitInformatieObject(BesluitTypeA);

        var result = await GetHandler(Client(AuthorizationScopes.Besluiten.Read, BesluitTypeA))
            .Handle(new GetBesluitInformatieObjectQuery { Id = id }, CancellationToken.None);

        Assert.Equal(QueryStatus.OK, result.Status);
    }

    [Fact]
    public async Task Creating_a_relation_on_a_besluittype_the_client_is_not_authorized_for_is_forbidden_and_creates_nothing()
    {
        var besluit = await SeedBesluit(BesluitTypeA);

        var result = await CreateHandler(Client(AuthorizationScopes.Besluiten.Create, BesluitTypeB))
            .Handle(NewCreateCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        Assert.False(_context.BesluitInformatieObjecten.Any(b => b.BesluitId == besluit.Id));
    }

    [Fact]
    public async Task Creating_a_relation_on_an_authorized_besluittype_succeeds()
    {
        await SeedBesluit(BesluitTypeA);

        var result = await CreateHandler(Client(AuthorizationScopes.Besluiten.Create, BesluitTypeA))
            .Handle(NewCreateCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    [Fact]
    public async Task Deleting_a_relation_on_a_besluittype_the_client_is_not_authorized_for_is_forbidden_and_keeps_it()
    {
        var id = await SeedBesluitInformatieObject(BesluitTypeA);

        var result = await DeleteHandler(Client(AuthorizationScopes.Besluiten.Delete, BesluitTypeB))
            .Handle(new DeleteBesluitInformatieObjectCommand { Id = id }, CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        Assert.True(_context.BesluitInformatieObjecten.Any(b => b.Id == id));
    }

    [Fact]
    public async Task Deleting_a_relation_on_an_authorized_besluittype_removes_it()
    {
        var id = await SeedBesluitInformatieObject(BesluitTypeA);

        var result = await DeleteHandler(Client(AuthorizationScopes.Besluiten.Delete, BesluitTypeA))
            .Handle(new DeleteBesluitInformatieObjectCommand { Id = id }, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.False(_context.BesluitInformatieObjecten.Any(b => b.Id == id));
    }

    private async Task<Besluit> SeedBesluit(string besluitType)
    {
        var besluit = new Besluit
        {
            Id = Guid.NewGuid(),
            Owner = CurrentTenant,
            Identificatie = $"BESLUIT-{Guid.NewGuid():N}",
            VerantwoordelijkeOrganisatie = CurrentTenant,
            BesluitType = besluitType,
            Datum = DateOnly.FromDateTime(DateTime.UtcNow),
            CreationTime = DateTime.UtcNow,
            BesluitInformatieObjecten = [],
        };
        _context.Besluiten.Add(besluit);
        await _context.SaveChangesAsync();
        _uriService.Setup(u => u.GetId(BesluitUrl)).Returns(besluit.Id);
        return besluit;
    }

    private async Task<Guid> SeedBesluitInformatieObject(string besluitType)
    {
        var besluit = await SeedBesluit(besluitType);
        var besluitInformatieObject = new BesluitInformatieObject
        {
            Id = Guid.NewGuid(),
            Owner = CurrentTenant,
            Besluit = besluit,
            InformatieObject = $"http://documenten.local/api/v1/enkelvoudiginformatieobjecten/{Guid.NewGuid()}",
            CreationTime = DateTime.UtcNow,
        };
        _context.BesluitInformatieObjecten.Add(besluitInformatieObject);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return besluitInformatieObject.Id;
    }

    private static CreateBesluitInformatieObjectCommand NewCreateCommand() =>
        new()
        {
            BesluitInformatieObject = new BesluitInformatieObject
            {
                InformatieObject = $"http://documenten.local/api/v1/enkelvoudiginformatieobjecten/{Guid.NewGuid()}",
            },
            BesluitUrl = BesluitUrl,
        };

    private static IAuthorizationContextAccessor Client(string scope, string authorizedBesluitType)
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor
            .Setup(a => a.AuthorizationContext)
            .Returns(
                new AuthorizationContext(
                    new AuthorizedApplication
                    {
                        Rsin = CurrentTenant,
                        Authorizations = [new AuthorizationPermission { BesluitType = authorizedBesluitType, Scopes = [scope] }],
                    },
                    [scope]
                )
            );
        return accessor.Object;
    }

    private GetBesluitInformatieObjectQueryHandler GetHandler(IAuthorizationContextAccessor client) =>
        new(
            NullLogger<GetBesluitInformatieObjectQueryHandler>.Instance,
            _configuration,
            _uriService.Object,
            _context,
            client,
            _kenmerkenResolver.Object
        );

    private CreateBesluitInformatieObjectCommandHandler CreateHandler(IAuthorizationContextAccessor client) =>
        new(
            NullLogger<CreateBesluitInformatieObjectCommandHandler>.Instance,
            _configuration,
            _context,
            _uriService.Object,
            _businessRules.Object,
            Mock.Of<INotificatieService>(),
            _auditTrailFactory.Object,
            client,
            _kenmerkenResolver.Object
        );

    private DeleteBesluitInformatieObjectCommandHandler DeleteHandler(IAuthorizationContextAccessor client) =>
        new(
            NullLogger<DeleteBesluitInformatieObjectCommandHandler>.Instance,
            _configuration,
            _context,
            _uriService.Object,
            Mock.Of<INotificatieService>(),
            _auditTrailFactory.Object,
            client,
            _kenmerkenResolver.Object
        );
}
