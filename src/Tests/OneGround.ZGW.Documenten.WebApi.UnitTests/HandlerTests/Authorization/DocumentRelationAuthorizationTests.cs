using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Handlers.v1;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5;
using OneGround.ZGW.Documenten.Web.Services.FileValidation;
using Xunit;
using static OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization.DrcAuthorizationTestSupport;
using VertrouwelijkheidAanduiding = OneGround.ZGW.Common.DataModel.VertrouwelijkheidAanduiding;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization;

public class DocumentRelationAuthorizationTests : IDisposable
{
    private const string DocumentUrl = "http://documenten.local/api/v1/enkelvoudiginformatieobjecten/dddddddd-dddd-dddd-dddd-dddddddddddd";

    private static readonly string[] CreateScopes = [AuthorizationScopes.Documenten.Create];

    private readonly DrcDbContext _context = CreateDbContext();
    private readonly Mock<IEntityUriService> _uriService = new();
    private readonly Mock<IObjectInformatieObjectBusinessRuleService> _objectInformatieObjectRules = new();

    public DocumentRelationAuthorizationTests()
    {
        _objectInformatieObjectRules
            .Setup(r =>
                r.ValidateAsync(
                    It.IsAny<ObjectInformatieObject>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<List<ValidationError>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Creating_an_object_relation_on_a_type_the_client_is_not_authorized_for_is_forbidden_and_creates_nothing()
    {
        var document = await SeedAddressableDocument(TypeA);
        var client = Client(CreateScopes, Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Create));

        var result = await CreateObjectInformatieObjectHandler(client).Handle(NewObjectInformatieObjectCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        Assert.False(_context.ObjectInformatieObjecten.Any(o => o.InformatieObjectId == document.Id));
    }

    [Fact]
    public async Task Creating_an_object_relation_on_an_unauthorized_type_is_forbidden_before_relation_validation()
    {
        await SeedAddressableDocument(TypeA);
        var client = Client(CreateScopes, Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Create));
        _objectInformatieObjectRules
            .Setup(r =>
                r.ValidateAsync(
                    It.IsAny<ObjectInformatieObject>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<List<ValidationError>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<ObjectInformatieObject, string, bool, List<ValidationError>, CancellationToken>(
                (_, _, _, errors, _) =>
                    errors.Add(
                        new ValidationError("nonFieldErrors", ErrorCode.InconsistentRelation, "De combinatie informatieobject en object bestaat al.")
                    )
            )
            .ReturnsAsync(false);

        var result = await CreateObjectInformatieObjectHandler(client).Handle(NewObjectInformatieObjectCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        _objectInformatieObjectRules.Verify(
            r =>
                r.ValidateAsync(
                    It.IsAny<ObjectInformatieObject>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<List<ValidationError>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Creating_an_object_relation_on_an_authorized_type_succeeds()
    {
        await SeedAddressableDocument(TypeA);
        var client = Client(CreateScopes, Permission(TypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Create));

        var result = await CreateObjectInformatieObjectHandler(client).Handle(NewObjectInformatieObjectCommand(), CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    [Fact]
    public async Task Uploading_to_a_completed_bestandsdeel_of_an_unauthorized_type_is_forbidden_instead_of_returning_the_part()
    {
        var bestandsdeelId = await SeedBestandsdeel(TypeA, voltooid: true);
        var client = Client(CreateScopes, Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Create));

        var result = await UploadHandler(client).Handle(new UploadBestandsDeelCommand { BestandsDeelId = bestandsdeelId }, CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task Uploading_with_a_wrong_lock_to_an_unauthorized_type_is_forbidden_before_the_lock_is_checked()
    {
        var bestandsdeelId = await SeedBestandsdeel(TypeA, voltooid: false);
        var client = Client(CreateScopes, Permission(TypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Create));

        var result = await UploadHandler(client)
            .Handle(new UploadBestandsDeelCommand { BestandsDeelId = bestandsdeelId, Lock = "wrong" }, CancellationToken.None);

        Assert.Equal(CommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Uploading_to_a_completed_bestandsdeel_of_an_authorized_type_returns_it()
    {
        var bestandsdeelId = await SeedBestandsdeel(TypeA, voltooid: true);
        var client = Client(CreateScopes, Permission(TypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Documenten.Create));

        var result = await UploadHandler(client).Handle(new UploadBestandsDeelCommand { BestandsDeelId = bestandsdeelId }, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
    }

    private async Task<EnkelvoudigInformatieObject> SeedAddressableDocument(string type)
    {
        var document = await SeedDocumentAsync(_context, type);
        _uriService.Setup(u => u.GetId(DocumentUrl)).Returns(document.Id);
        return document;
    }

    private async Task<Guid> SeedBestandsdeel(string type, bool voltooid)
    {
        var document = await SeedDocumentAsync(_context, type);
        var bestandsdeel = new BestandsDeel
        {
            Id = Guid.NewGuid(),
            EnkelvoudigInformatieObjectVersieId = document.LatestEnkelvoudigInformatieObjectVersieId.Value,
            Volgnummer = 1,
            Omvang = 10,
            Voltooid = voltooid,
        };
        _context.BestandsDelen.Add(bestandsdeel);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return bestandsdeel.Id;
    }

    private static CreateObjectInformatieObjectCommand NewObjectInformatieObjectCommand() =>
        new()
        {
            ObjectInformatieObject = new ObjectInformatieObject
            {
                Object = $"http://zaken.local/api/v1/zaken/{Guid.NewGuid()}",
                ObjectType = ObjectType.zaak,
            },
            InformatieObjectUrl = DocumentUrl,
        };

    private CreateObjectInformatieObjectCommandHandler CreateObjectInformatieObjectHandler(IAuthorizationContextAccessor client) =>
        new(
            NullLogger<CreateObjectInformatieObjectCommandHandler>.Instance,
            CreateConfiguration(),
            _context,
            _uriService.Object,
            _objectInformatieObjectRules.Object,
            AuditTrailFactory().Object,
            client,
            KenmerkenResolver().Object
        );

    private UploadBestandsDeelCommandHandler UploadHandler(IAuthorizationContextAccessor client)
    {
        var resolver = new Mock<IDocumentServicesResolver>();
        resolver.Setup(r => r.GetDefault()).Returns(Mock.Of<IDocumentService>());

        return new UploadBestandsDeelCommandHandler(
            NullLogger<UploadBestandsDeelCommandHandler>.Instance,
            CreateConfiguration(),
            _uriService.Object,
            _context,
            client,
            resolver.Object,
            Mock.Of<INotificatieService>(),
            KenmerkenResolver().Object,
            Mock.Of<IFileValidationService>(),
            AuditTrailFactory().Object
        );
    }
}
