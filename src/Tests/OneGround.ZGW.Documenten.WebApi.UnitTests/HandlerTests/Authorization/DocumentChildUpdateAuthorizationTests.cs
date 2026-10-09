using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1._5;
using OneGround.ZGW.Documenten.Web.Handlers;
using OneGround.ZGW.Documenten.Web.Handlers.v1;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5;
using Xunit;
using static OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization.DrcAuthorizationTestSupport;
using VertrouwelijkheidAanduiding = OneGround.ZGW.Common.DataModel.VertrouwelijkheidAanduiding;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization;

public class DocumentChildUpdateAuthorizationTests : IAsyncLifetime
{
    private const string DocumentAUrl = "http://documenten.local/api/v1/enkelvoudiginformatieobjecten/aaaaaaaa-0000-0000-0000-000000000001";
    private const string DocumentBUrl = "http://documenten.local/api/v1/enkelvoudiginformatieobjecten/bbbbbbbb-0000-0000-0000-000000000002";

    private static readonly string[] UpdateScopes = [AuthorizationScopes.Documenten.Update];

    private readonly DrcDbContext _context = CreateDbContext();
    private readonly Mock<IEntityUriService> _uriService = new();
    private readonly Mock<IAuditTrailFactory> _auditTrailFactory = AuditTrailFactory();
    private EnkelvoudigInformatieObject _documentA;
    private EnkelvoudigInformatieObject _documentB;

    public async Task InitializeAsync()
    {
        _documentA = await SeedDocumentAsync(_context, TypeA);
        _documentB = await SeedDocumentAsync(_context, TypeB);
        _uriService.Setup(u => u.GetId(DocumentAUrl)).Returns(_documentA.Id);
        _uriService.Setup(u => u.GetId(DocumentBUrl)).Returns(_documentB.Id);
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static AuthorizationPermission UpdateOn(string type) =>
        Permission(type, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Update);

    // --- GebruiksRecht ---

    [Fact]
    public async Task Updating_a_gebruiksrecht_of_a_document_the_client_is_not_authorized_for_is_forbidden()
    {
        var id = await SeedGebruiksRecht(_documentA.Id);

        var result = await UpdateGebruiksRechtAsync(id, DocumentAUrl, Client(UpdateScopes, UpdateOn(TypeB)));

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        _auditTrailFactory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Moving_a_gebruiksrecht_to_another_document_is_not_allowed()
    {
        var id = await SeedGebruiksRecht(_documentA.Id);

        var result = await UpdateGebruiksRechtAsync(id, DocumentBUrl, Client(UpdateScopes, UpdateOn(TypeA), UpdateOn(TypeB)));

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        var error = Assert.Single(result.Errors);
        Assert.Equal("informatieobject", error.Name);
        Assert.Equal(ErrorCode.UpdateNotAllowed, error.Code);
        _context.ChangeTracker.Clear();
        Assert.Equal(_documentA.Id, _context.GebruiksRechten.Single(g => g.Id == id).InformatieObjectId);
    }

    [Fact]
    public async Task Moving_a_gebruiksrecht_away_from_an_unauthorized_document_is_forbidden_before_validation()
    {
        var id = await SeedGebruiksRecht(_documentA.Id);

        var result = await UpdateGebruiksRechtAsync(id, DocumentBUrl, Client(UpdateScopes, UpdateOn(TypeB)));

        Assert.Equal(CommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Resubmitting_the_same_informatieobject_is_allowed()
    {
        var id = await SeedGebruiksRecht(_documentA.Id);

        var result = await UpdateGebruiksRechtAsync(id, DocumentAUrl, Client(UpdateScopes, UpdateOn(TypeA)));

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    // --- Verzending ---

    [Fact]
    public async Task Moving_a_verzending_away_from_a_document_the_client_is_not_authorized_for_is_forbidden()
    {
        var id = await SeedVerzending(_documentA.Id);

        var result = await UpdateVerzendingAsync(id, DocumentBUrl, Client(UpdateScopes, UpdateOn(TypeB)));

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        _context.ChangeTracker.Clear();
        Assert.Equal(_documentA.Id, _context.Verzendingen.Single(v => v.Id == id).InformatieObjectId);
    }

    [Fact]
    public async Task Moving_a_verzending_to_a_document_the_client_is_not_authorized_for_is_forbidden()
    {
        var id = await SeedVerzending(_documentA.Id);

        var result = await UpdateVerzendingAsync(id, DocumentBUrl, Client(UpdateScopes, UpdateOn(TypeA)));

        Assert.Equal(CommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Moving_a_verzending_between_two_authorized_documents_succeeds()
    {
        var id = await SeedVerzending(_documentA.Id);

        var result = await UpdateVerzendingAsync(id, DocumentBUrl, Client(UpdateScopes, UpdateOn(TypeA), UpdateOn(TypeB)));

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    private async Task<Guid> SeedGebruiksRecht(Guid documentId)
    {
        var gebruiksRecht = new GebruiksRecht
        {
            Id = Guid.NewGuid(),
            InformatieObjectId = documentId,
            OmschrijvingVoorwaarden = "voorwaarden",
            Startdatum = DateTime.UtcNow,
            CreationTime = DateTime.UtcNow,
        };
        _context.GebruiksRechten.Add(gebruiksRecht);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return gebruiksRecht.Id;
    }

    private async Task<Guid> SeedVerzending(Guid documentId)
    {
        var verzending = new Verzending
        {
            Id = Guid.NewGuid(),
            InformatieObjectId = documentId,
            Betrokkene = "https://betrokkene.example.com/1",
            AardRelatie = AardRelatie.afzender,
            Contactpersoon = "https://contactpersoon.example.com/1",
            CreationTime = DateTime.UtcNow,
        };
        _context.Verzendingen.Add(verzending);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return verzending.Id;
    }

    private Task<CommandResult<GebruiksRecht>> UpdateGebruiksRechtAsync(Guid id, string informatieObjectUrl, IAuthorizationContextAccessor client) =>
        new UpdateGebruiksRechtCommandHandler(
            NullLogger<UpdateGebruiksRechtCommandHandler>.Instance,
            CreateConfiguration(),
            _context,
            _uriService.Object,
            Mock.Of<IEntityUpdater<GebruiksRecht>>(),
            Mock.Of<INotificatieService>(),
            _auditTrailFactory.Object,
            client,
            KenmerkenResolver().Object,
            Mock.Of<IGenericObjectMergerFactory>()
        ).Handle(
            new UpdateGebruiksRechtCommand
            {
                Id = id,
                InformatieObjectUrl = informatieObjectUrl,
                GebruiksRecht = new GebruiksRecht { OmschrijvingVoorwaarden = "gewijzigd", Startdatum = DateTime.UtcNow },
            },
            CancellationToken.None
        );

    private Task<CommandResult<Verzending>> UpdateVerzendingAsync(Guid id, string informatieObjectUrl, IAuthorizationContextAccessor client)
    {
        var businessRules = new Mock<IVerzendingBusinessRuleService>();
        businessRules
            .Setup(r =>
                r.Validate(
                    It.IsAny<EnkelvoudigInformatieObject>(),
                    It.IsAny<Verzending>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<decimal>(),
                    It.IsAny<List<ValidationError>>()
                )
            )
            .Returns(true);

        return new UpdateVerzendingCommandHandler(
            NullLogger<UpdateVerzendingCommandHandler>.Instance,
            CreateConfiguration(),
            _context,
            _uriService.Object,
            Mock.Of<IEntityUpdater<Verzending>>(),
            Mock.Of<INotificatieService>(),
            _auditTrailFactory.Object,
            client,
            businessRules.Object,
            KenmerkenResolver().Object,
            Mock.Of<IGenericObjectMergerFactory>()
        ).Handle(
            new UpdateVerzendingCommand
            {
                Id = id,
                InformatieObjectUrl = informatieObjectUrl,
                Version = 1.5M,
                Verzending = new Verzending
                {
                    Betrokkene = "https://betrokkene.example.com/1",
                    AardRelatie = AardRelatie.afzender,
                    Contactpersoon = "https://contactpersoon.example.com/1",
                },
            },
            CancellationToken.None
        );
    }
}
