using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Handlers.v1._1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.EnkelvoudigeInformatieHandlerTests;

public class CreateEnkelvoudigInformatieObjectVersionsTests : EnkelvoudigInformatieObjectVersionsBase<CreateEnkelvoudigInformatieObjectCommandHandler>
{
    [Fact]
    public async Task Create_Base64_Document_Should_Send_Notification()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Q=",
                    "added_smalldocument.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f"), 8));

        var handler = CreateHandler();

        // Act

        CreateEnkelvoudigInformatieObjectCommand command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                // Test-part
                Inhoud = "VW5pdFRlc3Q=",
                Bestandsomvang = 8,
                Bestandsnaam = "added_smalldocument.txt",
                // Other
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,
                // Parent EnkelvoudigInformatieObject
                EnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d => d.Bestandsnaam == "added_smalldocument.txt");
        Assert.NotNull(addedDocument);

        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Create_MetaOnly_Document_Should_Send_Notification(string inhoud)
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:99999999-9999-9999-9999-999999999999"), 9999));

        var handler = CreateHandler();

        // Act

        CreateEnkelvoudigInformatieObjectCommand command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                // Test-part
                Inhoud = inhoud,
                Bestandsomvang = 0,
                Bestandsnaam = "OnlyMetaDocument",
                // Other
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,

                // Parent EnkelvoudigInformatieObject
                EnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d => d.Bestandsnaam == "OnlyMetaDocument");
        Assert.NotNull(addedDocument);

        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Create_Large_Document_Should_Not_send_Notification(string inhoud)
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m => m.InitiateMultipartUploadAsync("added_largedocument.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MultiPartDocument("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0="));

        var handler = CreateHandler();

        // Act

        CreateEnkelvoudigInformatieObjectCommand command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                // Test-part
                Inhoud = inhoud,
                Bestandsomvang = 2_500_000,
                Bestandsnaam = "added_largedocument.pdf",
                // Other
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,
                // Parent EnkelvoudigInformatieObject
                EnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d => d.Bestandsnaam == "added_largedocument.pdf");
        Assert.NotNull(addedDocument);

        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Never);
    }

    [Fact]
    public async Task Create_Base64_Document_Should_Add_Document_v1_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Q=",
                    "added_smalldocument.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f"), 8));

        var handler = CreateHandler();

        // Act

        CreateEnkelvoudigInformatieObjectCommand command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                // Test-part
                Inhoud = "VW5pdFRlc3Q=",
                Bestandsomvang = 8,
                Bestandsnaam = "added_smalldocument.txt",
                // Other
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,
                // Parent EnkelvoudigInformatieObject
                EnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d => d.Bestandsnaam == "added_smalldocument.txt");
        Assert.NotNull(addedDocument);
        Assert.Equal(1, addedDocument.Versie);
        Assert.Equal(8, addedDocument.Bestandsomvang);
        Assert.False(addedDocument.EnkelvoudigInformatieObject.Locked);
        Assert.Null(addedDocument.EnkelvoudigInformatieObject.Lock);
        Assert.Equal("urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f", addedDocument.Inhoud);
        Assert.Empty(addedDocument.BestandsDelen);

        _mockDocumentService.Verify(
            m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Q=",
                    "added_smalldocument.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Create_MetaOnly_Document_Should_Add_Document_v1_Only_In_Database(string inhoud)
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:99999999-9999-9999-9999-999999999999"), 9999));

        var handler = CreateHandler();

        // Act

        CreateEnkelvoudigInformatieObjectCommand command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                // Test-part
                Inhoud = inhoud,
                Bestandsomvang = 0,
                Bestandsnaam = "OnlyMetaDocument",
                // Other
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,

                // Parent EnkelvoudigInformatieObject
                EnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d => d.Bestandsnaam == "OnlyMetaDocument");
        Assert.NotNull(addedDocument);
        Assert.Equal(1, addedDocument.Versie);
        Assert.Equal(0, addedDocument.Bestandsomvang);
        Assert.False(addedDocument.EnkelvoudigInformatieObject.Locked);
        Assert.Null(addedDocument.EnkelvoudigInformatieObject.Lock);
        Assert.Null(addedDocument.Inhoud);
        Assert.Empty(addedDocument.BestandsDelen);

        _mockDocumentService.Verify(
            m =>
                m.AddDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Create_Large_Document_Should_Add_Document_v1_And_Add_Bestandsdelen_In_Database(string inhoud)
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m => m.InitiateMultipartUploadAsync("added_largedocument.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MultiPartDocument("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0="));

        var handler = CreateHandler();

        // Act

        CreateEnkelvoudigInformatieObjectCommand command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                // Test-part
                Inhoud = inhoud,
                Bestandsomvang = 2_500_000,
                Bestandsnaam = "added_largedocument.pdf",
                // Other
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,
                // Parent EnkelvoudigInformatieObject
                EnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d => d.Bestandsnaam == "added_largedocument.pdf");
        Assert.NotNull(addedDocument);

        Assert.Equal(1, addedDocument.Versie);
        Assert.Equal(2_500_000, addedDocument.Bestandsomvang);
        Assert.True(addedDocument.EnkelvoudigInformatieObject.Locked);
        Assert.NotEmpty(addedDocument.EnkelvoudigInformatieObject.Lock);
        Assert.Null(addedDocument.Inhoud); // Note: because not checked-in
        Assert.Equal("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0=", addedDocument.MultiPartDocumentId);
        Assert.Equal(3, addedDocument.BestandsDelen.Count);

        _mockDocumentService.Verify(
            m => m.InitiateMultipartUploadAsync("added_largedocument.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockDocumentService.Verify(
            m =>
                m.AddDocumentAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Never);
    }

    private CreateEnkelvoudigInformatieObjectCommandHandler CreateHandler()
    {
        return new CreateEnkelvoudigInformatieObjectCommandHandler(
            logger: _mockLogger.Object,
            configuration: _configuration,
            context: _mockDbContext,
            uriService: _mockUriService.Object,
            nummerGenerator: _mockNummerGenerator.Object,
            documentServicesResolver: _mockDocumentServicesResolver.Object,
            enkelvoudigInformatieObjectBusinessRuleService: _mockEnkvoudigInfObjBusinessRuleService.Object,
            catalogiServiceAgent: _mockCatalogiServiceAgent.Object,
            auditTrailFactory: _mockAuditTrailFactory.Object,
            authorizationContextAccessor: _mockAuthorizationContextAccessor.Object,
            lockGenerator: _mockLockGenerator.Object,
            formOptions: _mockFormOptions.Object,
            notificatieService: _mockNotificatieService.Object
        );
    }
}
