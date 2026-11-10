using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
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

    [Fact]
    public async Task Update_with_a_status_of_the_same_zaak_sets_the_status()
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var zaakId = _context.ZaakInformatieObjecten.Single(z => z.Id == id).ZaakId;
        var statusId = await SeedStatusAsync(zaakId);

        var result = await UpdateWithStatusAsync(id, statusId);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal(statusId, _context.ZaakInformatieObjecten.Single(z => z.Id == id).StatusId);
    }

    [Fact]
    public async Task Update_with_a_status_of_another_zaak_is_rejected()
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var otherZaak = NewZaak(CurrentTenant, ZaakTypeA);
        _context.Zaken.Add(otherZaak);
        await _context.SaveChangesAsync();
        var statusId = await SeedStatusAsync(otherZaak.Id);

        var result = await UpdateWithStatusAsync(id, statusId);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Contains(result.Errors, e => e.Name == "status" && e.Code == ErrorCode.Invalid);
        Assert.Null(_context.ZaakInformatieObjecten.Single(z => z.Id == id).StatusId);
    }

    [Fact]
    public async Task Update_keeping_a_stored_status_of_another_zaak_is_accepted()
    {
        var id = await SeedZaakInformatieObjectAsync(ZaakTypeA);
        var otherZaak = NewZaak(CurrentTenant, ZaakTypeA);
        _context.Zaken.Add(otherZaak);
        await _context.SaveChangesAsync();
        var statusId = await SeedStatusAsync(otherZaak.Id);
        _context.ZaakInformatieObjecten.Single(z => z.Id == id).StatusId = statusId;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await UpdateWithStatusAsync(id, statusId);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal(statusId, _context.ZaakInformatieObjecten.Single(z => z.Id == id).StatusId);
    }

    [Fact]
    public async Task Create_with_a_status_of_the_same_zaak_sets_the_status()
    {
        var zaak = await SeedZaakAsync();
        var statusId = await SeedStatusAsync(zaak.Id);

        var result = await CreateWithStatusAsync(zaak.Id, statusId);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal(statusId, result.Result.StatusId);
    }

    [Fact]
    public async Task Create_with_a_status_of_another_zaak_is_rejected()
    {
        var zaak = await SeedZaakAsync();
        var otherZaak = await SeedZaakAsync();
        var statusId = await SeedStatusAsync(otherZaak.Id);

        var result = await CreateWithStatusAsync(zaak.Id, statusId);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Contains(result.Errors, e => e.Name == "status" && e.Code == ErrorCode.Invalid);
        Assert.False(_context.ZaakInformatieObjecten.Any(z => z.ZaakId == zaak.Id));
    }

    private async Task<Zaak> SeedZaakAsync()
    {
        var zaak = NewZaak(CurrentTenant, ZaakTypeA);
        _context.Zaken.Add(zaak);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return zaak;
    }

    private Task<CommandResult<ZaakInformatieObject>> CreateWithStatusAsync(Guid zaakId, Guid statusId)
    {
        const string statusUrl = "http://zaken.local/api/v1/statussen/status-under-test";
        const string informatieObjectType = "http://catalogi.local/api/v1/informatieobjecttypen/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetId(ZaakUrl)).Returns(zaakId);
        uriService.Setup(s => s.GetId(statusUrl)).Returns(statusId);
        var catalogi = new Mock<ICatalogiServiceAgent>();
        catalogi
            .Setup(a => a.GetZaakTypeByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(new ZaakTypeResponseDto { InformatieObjectTypen = [informatieObjectType] }));
        var documenten = new Mock<ICachedDocumentenServiceAgent>();
        documenten
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(
                    new EnkelvoudigInformatieObjectResponseDto { InformatieObjectType = informatieObjectType }
                )
            );
        var client = Client(UpdateScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        return new V1_5.CreateZaakInformatieObjectCommandHandler(
            NullLogger<V1_5.CreateZaakInformatieObjectCommandHandler>.Instance,
            CreateConfiguration(),
            _context,
            _businessRules.Object,
            Mock.Of<INotificatieService>(),
            _auditTrailFactory.Object,
            uriService.Object,
            _closedZaakRule.Object,
            catalogi.Object,
            documenten.Object,
            client,
            KenmerkenResolver().Object
        ).Handle(
            new V1_5.CreateZaakInformatieObjectCommand
            {
                ZaakInformatieObject = new ZaakInformatieObject
                {
                    Id = Guid.NewGuid(),
                    InformatieObject = $"http://documenten.local/api/v1/enkelvoudiginformatieobjecten/{Guid.NewGuid()}",
                    Titel = "new titel",
                    CreationTime = DateTime.UtcNow,
                },
                ZaakUrl = ZaakUrl,
                StatusUrl = statusUrl,
            },
            CancellationToken.None
        );
    }

    private async Task<Guid> SeedStatusAsync(Guid zaakId)
    {
        var status = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Owner = CurrentTenant,
            ZaakId = zaakId,
            StatusType = "http://catalogi.local/api/v1/statustypen/dddddddd-dddd-dddd-dddd-dddddddddddd",
            DatumStatusGezet = DateTime.UtcNow,
            CreationTime = DateTime.UtcNow,
        };

        _context.ZaakStatussen.Add(status);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return status.Id;
    }

    private Task<CommandResult<ZaakInformatieObject>> UpdateWithStatusAsync(Guid id, Guid statusId)
    {
        const string statusUrl = "http://zaken.local/api/v1/statussen/status-under-test";
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetId(statusUrl)).Returns(statusId);
        _businessRules
            .Setup(r =>
                r.ValidateAsync(
                    It.IsAny<ZaakInformatieObject>(),
                    It.IsAny<ZaakInformatieObject>(),
                    It.IsAny<string>(),
                    It.IsAny<List<ValidationError>>()
                )
            )
            .ReturnsAsync((ZaakInformatieObject _, ZaakInformatieObject _, string _, List<ValidationError> errors) => errors.Count == 0);
        var client = Client(UpdateScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        return new V1_5.UpdateZaakInformatieObjectCommandHandler(
            NullLogger<V1_5.UpdateZaakInformatieObjectCommandHandler>.Instance,
            CreateConfiguration(),
            _context,
            uriService.Object,
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
                ZaakInformatieObject = new ZaakInformatieObject { Titel = "changed titel" },
                ZaakUrl = ZaakUrl,
                StatusUrl = statusUrl,
            },
            CancellationToken.None
        );
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
