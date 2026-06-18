using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Concurrency;
using OneGround.ZGW.Documenten.Web.Handlers.v1._1;
using Polly;
using Xunit;
using CreateV1_5Command = OneGround.ZGW.Documenten.Web.Handlers.v1._5.CreateEnkelvoudigInformatieObjectCommand;
using CreateV1_5Handler = OneGround.ZGW.Documenten.Web.Handlers.v1._5.CreateEnkelvoudigInformatieObjectCommandHandler;
using CreateV1Command = OneGround.ZGW.Documenten.Web.Handlers.v1.CreateEnkelvoudigInformatieObjectCommand;
using CreateV1Handler = OneGround.ZGW.Documenten.Web.Handlers.v1.CreateEnkelvoudigInformatieObjectCommandHandler;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.EnkelvoudigeInformatieHandlerTests;

public class CreateEnkelvoudigInformatieObjectVersionsTests : EnkelvoudigInformatieObjectVersionsBase<CreateEnkelvoudigInformatieObjectCommandHandler>
{
    public CreateEnkelvoudigInformatieObjectVersionsTests(TestMocksFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Create_Base64_Document_Should_Send_Notification()
    {
        // Arrange

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
                InformatieObject = new EnkelvoudigInformatieObject
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
        // Arrange

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
                InformatieObject = new EnkelvoudigInformatieObject
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
        // Arrange

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
                InformatieObject = new EnkelvoudigInformatieObject
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
        // Arrange

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
                InformatieObject = new EnkelvoudigInformatieObject
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
        Assert.False(addedDocument.InformatieObject.Locked);
        Assert.Null(addedDocument.InformatieObject.Lock);
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
        // Arrange

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
                InformatieObject = new EnkelvoudigInformatieObject
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
        Assert.False(addedDocument.InformatieObject.Locked);
        Assert.Null(addedDocument.InformatieObject.Lock);
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
        // Arrange

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
                InformatieObject = new EnkelvoudigInformatieObject
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
        Assert.True(addedDocument.InformatieObject.Locked);
        Assert.NotEmpty(addedDocument.InformatieObject.Lock);
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

    [Fact]
    public async Task CreateV1_1_SetsLatestVertrouwelijkheidAanduiding_ToVersieValue()
    {
        await SetupMocksAsync();

        var handler = CreateHandler();

        var command = new CreateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                Inhoud = null,
                Bestandsomvang = 0,
                Bestandsnaam = "vha_test_v1_1.txt",
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.intern,
                InformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        var savedEio = _mockDbContext.EnkelvoudigInformatieObjecten.Single(e => e.Id == result.Result.InformatieObject.Id);
        Assert.Equal(VertrouwelijkheidAanduiding.intern, savedEio.LatestVertrouwelijkheidAanduiding);
    }

    [Fact]
    public async Task CreateV1_SetsLatestVertrouwelijkheidAanduiding_ToVersieValue()
    {
        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    It.IsAny<string>(),
                    "vha_test_v1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:11111111-1111-1111-1111-111111111111"), 0));

        var handler = BuildV1CreateHandler();

        var command = new CreateV1Command
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                Inhoud = null,
                Bestandsomvang = 0,
                Bestandsnaam = "vha_test_v1.txt",
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.intern,
                InformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        var savedEio = _mockDbContext.EnkelvoudigInformatieObjecten.Single(e => e.Id == result.Result.InformatieObject.Id);
        Assert.Equal(VertrouwelijkheidAanduiding.intern, savedEio.LatestVertrouwelijkheidAanduiding);
    }

    [Fact]
    public async Task CreateV1_5_SetsLatestVertrouwelijkheidAanduiding_ToVersieValue()
    {
        await SetupMocksAsync();

        var handler = BuildV1_5CreateHandler();

        var command = new CreateV1_5Command
        {
            EnkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
            {
                Inhoud = null,
                Bestandsomvang = 0,
                Bestandsnaam = "vha_test_v1_5.txt",
                Bronorganisatie = "000001375",
                Formaat = "raw",
                Taal = "eng",
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.intern,
                InformatieObject = new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f",
                },
            },
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        var savedEio = _mockDbContext.EnkelvoudigInformatieObjecten.Single(e => e.Id == result.Result.InformatieObject.Id);
        Assert.Equal(VertrouwelijkheidAanduiding.intern, savedEio.LatestVertrouwelijkheidAanduiding);
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
            notificatieService: _mockNotificatieService.Object,
            documentKenmerkenResolver: _mockDocumentKenmerkenResolver.Object
        );
    }

    private CreateV1Handler BuildV1CreateHandler()
    {
        var mockOptionsMonitor = new Mock<IOptionsMonitor<HttpRetryStrategyOptions>>();
        mockOptionsMonitor
            .Setup(m => m.CurrentValue)
            .Returns(
                new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(1),
                }
            );

        // v1 handler is internal — use NullLogger to avoid DynamicProxyGenAssembly2 visibility issue
        return new CreateV1Handler(
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateV1Handler>.Instance,
            configuration: _configuration,
            context: _mockDbContext,
            uriService: _mockUriService.Object,
            nummerGenerator: _mockNummerGenerator.Object,
            documentServicesResolver: _mockDocumentServicesResolver.Object,
            enkelvoudigInformatieObjectBusinessRuleService: _mockEnkvoudigInfObjBusinessRuleService.Object,
            notificatieService: _mockNotificatieService.Object,
            catalogiServiceAgent: _mockCatalogiServiceAgent.Object,
            auditTrailFactory: _mockAuditTrailFactory.Object,
            authorizationContextAccessor: _mockAuthorizationContextAccessor.Object,
            documentKenmerkenResolver: _mockDocumentKenmerkenResolver.Object,
            entityMergerFactory: _mockEntityMergerFactory.Object,
            concurrencyRetryPipeline: new ResilienceConcurrencyRetryPipeline<EnkelvoudigInformatieObject>(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ResilienceConcurrencyRetryPipeline<EnkelvoudigInformatieObject>>.Instance,
                mockOptionsMonitor.Object
            )
        );
    }

    private CreateV1_5Handler BuildV1_5CreateHandler()
    {
        var mockV1_5Logger = new Mock<ILogger<CreateV1_5Handler>>();

        return new CreateV1_5Handler(
            logger: mockV1_5Logger.Object,
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
            notificatieService: _mockNotificatieService.Object,
            documentKenmerkenResolver: _mockDocumentKenmerkenResolver.Object
        );
    }
}
