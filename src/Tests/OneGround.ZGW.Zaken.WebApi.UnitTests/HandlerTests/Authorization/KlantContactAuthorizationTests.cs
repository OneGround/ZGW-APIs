using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization;

public class KlantContactAuthorizationTests : IDisposable
{
    private const string ZaakUrl = "http://zaken.local/api/v1/zaken/dddddddd-dddd-dddd-dddd-dddddddddddd";

    private static readonly string[] ReadScopes = [AuthorizationScopes.Zaken.Read];
    private static readonly string[] CreateScopes = [AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate];

    private readonly ZrcDbContext _context = CreateDbContext();
    private readonly Mock<IClosedZaakModificationBusinessRule> _closedZaakRule = new();
    private readonly Mock<IEntityUriService> _uriService = new();

    public KlantContactAuthorizationTests()
    {
        _closedZaakRule.Setup(r => r.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>())).Returns(true);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task KlantContact_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden()
    {
        var id = await SeedKlantContact(ZaakTypeA);
        var client = Client(ReadScopes, Permission(ZaakTypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Read));

        var result = await GetHandler(client).Handle(new GetKlantContactQuery { Id = id }, CancellationToken.None);

        Assert.Equal(QueryStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task KlantContact_of_an_authorized_zaaktype_is_returned()
    {
        var id = await SeedKlantContact(ZaakTypeA);
        var client = Client(ReadScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Read));

        var result = await GetHandler(client).Handle(new GetKlantContactQuery { Id = id }, CancellationToken.None);

        Assert.Equal(QueryStatus.OK, result.Status);
    }

    [Fact]
    public async Task Creating_a_KlantContact_on_a_zaaktype_the_client_is_not_authorized_for_is_forbidden_and_creates_nothing()
    {
        var zaak = await SeedZaak(ZaakTypeA);
        var client = Client(CreateScopes, Permission(ZaakTypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        var result = await CreateHandler(client).Handle(NewCreateCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        _closedZaakRule.Verify(r => r.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>()), Times.Never);
        Assert.False(_context.KlantContacten.Any(k => k.ZaakId == zaak.Id));
    }

    [Fact]
    public async Task Creating_a_KlantContact_on_an_authorized_zaaktype_succeeds()
    {
        await SeedZaak(ZaakTypeA);
        var client = Client(CreateScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        var result = await CreateHandler(client).Handle(NewCreateCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    private async Task<Zaak> SeedZaak(string zaakType)
    {
        var zaak = NewZaak(CurrentTenant, zaakType);
        _context.Zaken.Add(zaak);
        await _context.SaveChangesAsync();
        _uriService.Setup(u => u.GetId(ZaakUrl)).Returns(zaak.Id);
        return zaak;
    }

    private async Task<Guid> SeedKlantContact(string zaakType)
    {
        var zaak = await SeedZaak(zaakType);
        var klantContact = new KlantContact
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Identificatie = $"KLANTCONTACT-{Guid.NewGuid():N}",
            DatumTijd = DateTime.UtcNow,
            CreationTime = DateTime.UtcNow,
        };
        _context.KlantContacten.Add(klantContact);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return klantContact.Id;
    }

    private static CreateKlantContactCommand NewCreateCommand() =>
        new()
        {
            KlantContact = new KlantContact { Identificatie = $"KLANTCONTACT-{Guid.NewGuid():N}", DatumTijd = DateTime.UtcNow },
            ZaakUrl = ZaakUrl,
        };

    private GetKlantContactQueryHandler GetHandler(IAuthorizationContextAccessor client) =>
        new(
            NullLogger<GetKlantContactQueryHandler>.Instance,
            CreateConfiguration(),
            _uriService.Object,
            _context,
            client,
            KenmerkenResolver().Object
        );

    private CreateKlantContactCommandHandler CreateHandler(IAuthorizationContextAccessor client) =>
        new(
            NullLogger<CreateKlantContactCommandHandler>.Instance,
            CreateConfiguration(),
            Mock.Of<INotificatieService>(),
            _context,
            _uriService.Object,
            _closedZaakRule.Object,
            AuditTrailFactory().Object,
            Mock.Of<INummerGenerator>(),
            client,
            KenmerkenResolver().Object
        );
}
