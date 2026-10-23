using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;
using V1 = OneGround.ZGW.Zaken.Web.Handlers.v1;
using V1_5 = OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization;

public class MutateZaakInformatieObjectAuthorizationTests : IDisposable
{
    private const string ZaakUrl = "http://zaken.local/api/v1/zaken/cccccccc-cccc-cccc-cccc-cccccccccccc";

    private static readonly string[] UpdateScopes = [AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate];
    private static readonly string[] DeleteScopes =
    [
        AuthorizationScopes.Zaken.Update,
        AuthorizationScopes.Zaken.ForcedUpdate,
        AuthorizationScopes.Zaken.Delete,
    ];

    private readonly ZrcDbContext _context = CreateDbContext();
    private readonly Mock<IAuditTrailFactory> _auditTrailFactory = AuditTrailFactory();
    private readonly Mock<IClosedZaakModificationBusinessRule> _closedZaakRule = new();
    private readonly Mock<IZaakInformatieObjectBusinessRuleService> _businessRules = new();

    public MutateZaakInformatieObjectAuthorizationTests()
    {
        _closedZaakRule.Setup(r => r.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>())).Returns(true);
        _businessRules
            .Setup(r =>
                r.ValidateAsync(
                    It.IsAny<ZaakInformatieObject>(),
                    It.IsAny<ZaakInformatieObject>(),
                    It.IsAny<string>(),
                    It.IsAny<List<ValidationError>>()
                )
            )
            .ReturnsAsync(true);
    }

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Update_on_a_zaaktype_the_client_is_not_authorized_for_is_forbidden_and_changes_nothing(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var client = Client(UpdateScopes, Permission(ZaakTypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        var result = await UpdateAsync(version, id, client);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        _closedZaakRule.Verify(r => r.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>()), Times.Never);
        _auditTrailFactory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never);
        Assert.Equal("original titel", _context.ZaakInformatieObjecten.Single(z => z.Id == id).Titel);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Update_on_an_authorized_zaaktype_succeeds(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var client = Client(UpdateScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        var result = await UpdateAsync(version, id, client);

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Delete_on_a_zaaktype_the_client_is_not_authorized_for_is_forbidden_and_keeps_the_relation(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var client = Client(DeleteScopes, Permission(ZaakTypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Delete));

        var result = await DeleteAsync(version, id, client);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        _closedZaakRule.Verify(r => r.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>()), Times.Never);
        Assert.True(_context.ZaakInformatieObjecten.Any(z => z.Id == id));
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Delete_on_an_authorized_zaaktype_removes_the_relation(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var client = Client(DeleteScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Delete));

        var result = await DeleteAsync(version, id, client);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.False(_context.ZaakInformatieObjecten.Any(z => z.Id == id));
    }

    private async Task<Guid> SeedZaakInformatieObjectAsync(string zaakType)
    {
        var zaak = NewZaak(CurrentTenant, zaakType);
        var zaakInformatieObject = new ZaakInformatieObject
        {
            Id = Guid.NewGuid(),
            Owner = CurrentTenant,
            Zaak = zaak,
            InformatieObject = $"http://documenten.local/api/v1/enkelvoudiginformatieobjecten/{Guid.NewGuid()}",
            Titel = "original titel",
            CreationTime = DateTime.UtcNow,
        };

        _context.Zaken.Add(zaak);
        _context.ZaakInformatieObjecten.Add(zaakInformatieObject);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return zaakInformatieObject.Id;
    }

    private Task<CommandResult<ZaakInformatieObject>> UpdateAsync(string version, Guid id, IAuthorizationContextAccessor client)
    {
        var update = new ZaakInformatieObject { Titel = "changed titel" };

        return version switch
        {
            "v1" => new V1.UpdateZaakInformatieObjectCommandHandler(
                NullLogger<V1.UpdateZaakInformatieObjectCommandHandler>.Instance,
                CreateConfiguration(),
                _context,
                Mock.Of<IEntityUriService>(),
                _businessRules.Object,
                Mock.Of<INotificatieService>(),
                Mock.Of<IEntityUpdater<ZaakInformatieObject>>(),
                _auditTrailFactory.Object,
                _closedZaakRule.Object,
                client,
                KenmerkenResolver().Object
            ).Handle(
                new V1.UpdateZaakInformatieObjectCommand
                {
                    Id = id,
                    ZaakInformatieObject = update,
                    ZaakUrl = ZaakUrl,
                },
                CancellationToken.None
            ),
            "v1.5" => new V1_5.UpdateZaakInformatieObjectCommandHandler(
                NullLogger<V1_5.UpdateZaakInformatieObjectCommandHandler>.Instance,
                CreateConfiguration(),
                _context,
                Mock.Of<IEntityUriService>(),
                _businessRules.Object,
                Mock.Of<INotificatieService>(),
                Mock.Of<IEntityUpdater<ZaakInformatieObject>>(),
                _auditTrailFactory.Object,
                _closedZaakRule.Object,
                client,
                KenmerkenResolver().Object
            ).Handle(
                new V1_5.UpdateZaakInformatieObjectCommand
                {
                    Id = id,
                    ZaakInformatieObject = update,
                    ZaakUrl = ZaakUrl,
                },
                CancellationToken.None
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
    }

    private Task<CommandResult> DeleteAsync(string version, Guid id, IAuthorizationContextAccessor client) =>
        version switch
        {
            "v1" => new V1.DeleteZaakInformatieObjectCommandHandler(
                NullLogger<V1.DeleteZaakInformatieObjectCommandHandler>.Instance,
                CreateConfiguration(),
                _context,
                Mock.Of<INotificatieService>(),
                _auditTrailFactory.Object,
                Mock.Of<IEntityUriService>(),
                _closedZaakRule.Object,
                client,
                KenmerkenResolver().Object
            ).Handle(new V1.DeleteZaakInformatieObjectCommand { Id = id }, CancellationToken.None),
            "v1.5" => new V1_5.DeleteZaakInformatieObjectCommandHandler(
                NullLogger<V1_5.DeleteZaakInformatieObjectCommandHandler>.Instance,
                CreateConfiguration(),
                _context,
                Mock.Of<INotificatieService>(),
                _auditTrailFactory.Object,
                Mock.Of<IEntityUriService>(),
                _closedZaakRule.Object,
                client,
                KenmerkenResolver().Object
            ).Handle(new V1_5.DeleteZaakInformatieObjectCommand { Id = id }, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
}
