using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Handlers.v1._1;
using OneGround.ZGW.Documenten.Web.MappingProfiles.v1._1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.EnkelvoudigeInformatieHandlerTests;

public class UpdateEnkelvoudigInformatieObjectVersionsTests : EnkelvoudigInformatieObjectVersionsBase<UpdateEnkelvoudigInformatieObjectCommandHandler>
{
    private readonly IRequestMerger _requestMerger;
    private readonly IMapper _mapper;

    public UpdateEnkelvoudigInformatieObjectVersionsTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new DomainToResponseProfile());
            config.AddProfile(new RequestToDomainProfile());
            config.ShouldMapMethod = (m => false);
        });

        _mapper = configuration.CreateMapper(t =>
        {
            if (t == typeof(MapLatestEnkelvoudigInformatieObjectVersieRequest))
            {
                return new MapLatestEnkelvoudigInformatieObjectVersieRequest();
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });

        _requestMerger = new RequestMerger(_mapper);
    }

    [Fact]
    public async Task Existing_Document_Update_With_Base64_Inhoud_Should_Send_Notification()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00"), 9));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_smalldocument.txt",
            "urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f",
            8
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': 'VW5pdFRlc3Qy', 'bestandsomvang': 9, 'bestandsnaam': 'updated_smalldocument_1.txt'}"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_smalldocument_1.txt"
        );

        Assert.NotNull(addedNewVersionOfDocument);

        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Once);
    }

    [Fact]
    public async Task Existing_Document_Update_With_MetaOnly_Document_Should_Send_Notification()
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

        var current = await SetupCurrentEnkelvoudigInformatieObject("OnlyMetaDocument", currentInhoud: null, currentBestandsomvang: 0);

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject("{ 'bestandsnaam': 'OnlyMetaDocument-2'}")
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "OnlyMetaDocument-2"
        );

        Assert.NotNull(addedNewVersionOfDocument);

        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Once);
    }

    [Fact]
    public async Task Existing_Document_Update_With_Large_Document_Should_Not_Send_Notification()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MultiPartDocument("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0="));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_largedocument.pdf",
            "urn:dms:unittest:12f97974-f0b1-47ec-bc43-a22dbc2d14fe",
            2_500_000
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': null, 'bestandsnaam': 'updated_largedocument_1.pdf', 'bestandsomvang': 2600000 }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_largedocument_1.pdf"
        );
        Assert.NotNull(addedNewVersionOfDocument);

        _mockNotificatieService.Verify(m => m.NotifyAsync(It.IsAny<Notification>(), default), Times.Never);
    }

    [Fact]
    public async Task Existing_Base64_Document_Update_With_Base64_Inhoud_And_Bestandsomvang_Should_Add_Document_v2_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00"), 9));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_smalldocument.txt",
            "urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f",
            8
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': 'VW5pdFRlc3Qy', 'bestandsomvang': 9, 'bestandsnaam': 'updated_smalldocument_1.txt'}"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_smalldocument_1.txt"
        );

        Assert.NotNull(addedNewVersionOfDocument);
        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(9, addedNewVersionOfDocument.Bestandsomvang);
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Equal("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00", addedNewVersionOfDocument.Inhoud);
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

        _mockDocumentService.Verify(
            m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Existing_Base64_Document_Keep_Base64_Inhoud_Only_Update_Bestandsnaam_Should_Not_Add_Document_In_Document_Store()
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

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_smalldocument.txt",
            "urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f",
            8
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'bestandsnaam': 'added_smalldocument_meta_changed_only.txt' }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        DumpVersies(); // tst

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "added_smalldocument_meta_changed_only.txt"
        );

        Assert.NotNull(addedNewVersionOfDocument);
        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(8, addedNewVersionOfDocument.Bestandsomvang);
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Equal("urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f", addedNewVersionOfDocument.Inhoud);
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

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

    [Fact]
    public async Task Existing_Large_Document_Document_Keep_Large_Document_Only_Update_Bestandsnaam_Should_Not_Add_Document_In_Document_Store()
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

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_largedocument.pdf",
            "urn:dms:unittest:12f97974-f0b1-47ec-bc43-a22dbc2d14fe",
            2_500_000
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'bestandsnaam': 'added_largedocument_meta_changed_only.pdf' }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        DumpVersies(); // tst

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "added_largedocument_meta_changed_only.pdf"
        );

        Assert.NotNull(addedNewVersionOfDocument);
        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(2_500_000, addedNewVersionOfDocument.Bestandsomvang);
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked); // Note: No bestandsdelen so there is no lock set
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock); // Note: No bestandsdelen so there is no lock value
        Assert.Equal("urn:dms:unittest:12f97974-f0b1-47ec-bc43-a22dbc2d14fe", addedNewVersionOfDocument.Inhoud);
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

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

    [Fact]
    public async Task Existing_Base64_Document_Update_With_Only_Bestandsnaam_Change_Should_Add_No_Document_In_Document_Store()
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

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_smalldocument.txt",
            "urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f",
            8
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject("{ 'bestandsnaam': 'updated_smalldocument_1.txt' }")
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_smalldocument_1.txt"
        );

        Assert.NotNull(addedNewVersionOfDocument);
        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(8, addedNewVersionOfDocument.Bestandsomvang); // The orignal document
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Equal("urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f", addedNewVersionOfDocument.Inhoud); // The orignal document urn
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

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

    [Fact]
    public async Task Existing_MetaOnly_Document_Update_With_MetaOnly_Document_Should_Add_Document_v2_Only_In_Database()
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

        var current = await SetupCurrentEnkelvoudigInformatieObject("OnlyMetaDocument", currentInhoud: null, currentBestandsomvang: 0);

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject("{ 'bestandsnaam': 'OnlyMetaDocument-2'}")
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "OnlyMetaDocument-2"
        );

        Assert.NotNull(addedNewVersionOfDocument);
        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(0, addedNewVersionOfDocument.Bestandsomvang);
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Null(addedNewVersionOfDocument.Inhoud);
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

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

    [Fact]
    public async Task Existing_Large_Document_Update_With_Large_Document_Should_Add_Bestandsdelen_In_Database_Add_Not_Add_Document_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MultiPartDocument("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0="));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_largedocument.pdf",
            "urn:dms:unittest:12f97974-f0b1-47ec-bc43-a22dbc2d14fe",
            2_500_000
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': null, 'bestandsnaam': 'updated_largedocument_1.pdf', 'bestandsomvang': 2600000 }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_largedocument_1.pdf"
        );
        Assert.NotNull(addedNewVersionOfDocument);

        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(2_600_000, addedNewVersionOfDocument.Bestandsomvang);
        Assert.True(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.NotEmpty(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Null(addedNewVersionOfDocument.Inhoud);
        Assert.Equal(
            "eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0=",
            addedNewVersionOfDocument.MultiPartDocumentId
        );
        Assert.Equal(3, addedNewVersionOfDocument.BestandsDelen.Count);

        _mockDocumentService.Verify(
            m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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

    [Fact]
    public async Task Existing_Large_Document_Update_With_Base64_Document_Should_Add_Document_v2_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00"), 9));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_largedocument.pdf",
            "urn:dms:unittest:12f97974-f0b1-47ec-bc43-a22dbc2d14fe",
            2_500_000
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                @"{ 'inhoud': 'VW5pdFRlc3Qy', 'bestandsnaam': 'updated_smalldocument_1.txt', 'bestandsomvang': 9 }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_smalldocument_1.txt"
        );
        Assert.NotNull(addedNewVersionOfDocument);

        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(9, addedNewVersionOfDocument.Bestandsomvang);
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Equal("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00", addedNewVersionOfDocument.Inhoud);
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

        _mockDocumentService.Verify(
            m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Existing_MetaOnly_Document_Update_With_Large_Document_Should_Add_Bestandsdelen_In_Database_Add_Not_Add_Document_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MultiPartDocument("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0="));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject("OnlyMetaDocument", currentInhoud: null, currentBestandsomvang: 0);

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': null, 'bestandsnaam': 'updated_largedocument_1.pdf', 'bestandsomvang': 2600000 }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_largedocument_1.pdf"
        );
        Assert.NotNull(addedNewVersionOfDocument);

        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(2_600_000, addedNewVersionOfDocument.Bestandsomvang);
        Assert.True(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.NotEmpty(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Null(addedNewVersionOfDocument.Inhoud);
        Assert.Equal(
            "eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0=",
            addedNewVersionOfDocument.MultiPartDocumentId
        );
        Assert.Equal(3, addedNewVersionOfDocument.BestandsDelen.Count);

        _mockDocumentService.Verify(
            m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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

    [Fact]
    public async Task Existing_MetaOnly_Document_Update_With_Base64_Document_Should_Add_Document_v2_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Document(new DocumentUrn("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00"), 9));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject("OnlyMetaDocument", currentInhoud: null, currentBestandsomvang: 0);

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': 'VW5pdFRlc3Qy', 'bestandsnaam': 'updated_smalldocument_1.txt', 'bestandsomvang': 9 }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_smalldocument_1.txt"
        );
        Assert.NotNull(addedNewVersionOfDocument);

        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(9, addedNewVersionOfDocument.Bestandsomvang);
        Assert.False(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.Null(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Equal("urn:dms:unittest:b143ec80-e893-413e-99a6-767492307e00", addedNewVersionOfDocument.Inhoud);
        Assert.Empty(addedNewVersionOfDocument.BestandsDelen);

        _mockDocumentService.Verify(
            m =>
                m.AddDocumentAsync(
                    "VW5pdFRlc3Qy",
                    "updated_smalldocument_1.txt",
                    It.IsAny<string>(),
                    It.IsAny<DocumentMeta>(),
                    true,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Existing_Base64_Document_Update_With_Large_Document_Should_Add_Bestandsdelen_In_Database_Add_Not_Add_Document_In_Document_Store()
    {
        // Setup

        await SetupMocksAsync();

        _mockDocumentService
            .Setup(m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MultiPartDocument("eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0="));

        var handler = CreateHandler();

        var current = await SetupCurrentEnkelvoudigInformatieObject(
            "added_smalldocument.txt",
            "urn:dms:unittest:f46545e7-0a79-4047-af30-ff5afa73916f",
            8
        );

        var merged = MergeWithCurrentEnkelvoudigInformatieObject(
            current,
            partialEnkelvoudigInformatieObjectRequest: JsonConvert.DeserializeObject(
                "{ 'inhoud': null, 'bestandsnaam': 'updated_largedocument_1.pdf', 'bestandsomvang': 2600000 }"
            )
        );

        var command = new UpdateEnkelvoudigInformatieObjectCommand
        {
            EnkelvoudigInformatieObjectVersie = merged,
            ExistingEnkelvoudigInformatieObjectId = current.Id,
        };

        // Act

        CommandResult<EnkelvoudigInformatieObjectVersie> result = await handler.Handle(command, new CancellationToken());

        // Assert

        Assert.Equal(CommandStatus.OK, result.Status);

        Assert.Equal(2, _mockDbContext.EnkelvoudigInformatieObjectVersies.Count());

        var addedNewVersionOfDocument = _mockDbContext.EnkelvoudigInformatieObjectVersies.SingleOrDefault(d =>
            d.Bestandsnaam == "updated_largedocument_1.pdf"
        );
        Assert.NotNull(addedNewVersionOfDocument);

        Assert.Equal(2, addedNewVersionOfDocument.Versie);
        Assert.Equal(2_600_000, addedNewVersionOfDocument.Bestandsomvang);
        Assert.True(addedNewVersionOfDocument.InformatieObject.Locked);
        Assert.NotEmpty(addedNewVersionOfDocument.InformatieObject.Lock);
        Assert.Null(addedNewVersionOfDocument.Inhoud);
        Assert.Equal(
            "eyJOYW1lIjoiMjAyMTA4IiwiS2V5IjoiYjllZTMyYzktMDUyMy00NTI0LWI1MDEtMzk0NDAwYzUwY2VjIn0=",
            addedNewVersionOfDocument.MultiPartDocumentId
        );
        Assert.Equal(3, addedNewVersionOfDocument.BestandsDelen.Count);

        _mockDocumentService.Verify(
            m => m.InitiateMultipartUploadAsync("updated_largedocument_1.pdf", It.IsAny<DocumentMeta>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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

    private EnkelvoudigInformatieObjectVersie MergeWithCurrentEnkelvoudigInformatieObject(
        EnkelvoudigInformatieObject currentEnkelvoudigInformatieObject,
        dynamic partialEnkelvoudigInformatieObjectRequest
    )
    {
        EnkelvoudigInformatieObjectUpdateRequestDto mergedEnkelvoudigInformatieObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            EnkelvoudigInformatieObjectUpdateRequestDto,
            EnkelvoudigInformatieObject
        >(currentEnkelvoudigInformatieObject, partialEnkelvoudigInformatieObjectRequest);

        EnkelvoudigInformatieObjectVersie mergedEnkelvoudigInformatieObjectVersie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(
            mergedEnkelvoudigInformatieObjectRequest
        );

        return mergedEnkelvoudigInformatieObjectVersie;
    }

    private async Task<EnkelvoudigInformatieObject> SetupCurrentEnkelvoudigInformatieObject(
        string currentBestandsnaam,
        string currentInhoud,
        long currentBestandsomvang
    )
    {
        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            Bestandsnaam = currentBestandsnaam,
            Inhoud = currentInhoud,
            Bestandsomvang = currentBestandsomvang,
            Versie = 1,
            Bronorganisatie = "000001375",
            Formaat = "raw",
            Taal = "eng",
            Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.openbaar,
            Owner = "813264571",
        };

        var currentEnkelvoudigInformatieObject = new EnkelvoudigInformatieObject
        {
            Id = new Guid("882ff9a0-d4c8-441c-b9ac-8f3f0f46f5b4"),
            CreationTime = DateTime.UtcNow,
            InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
            Owner = "813264571",
            Lock = null,
            Locked = false,
            LatestEnkelvoudigInformatieObjectVersie = enkelvoudigInformatieObjectVersie,
            EnkelvoudigInformatieObjectVersies = [enkelvoudigInformatieObjectVersie],
        };

        _mockDbContext.EnkelvoudigInformatieObjecten.Add(currentEnkelvoudigInformatieObject);
        await _mockDbContext.SaveChangesAsync();

        return currentEnkelvoudigInformatieObject;
    }

    private UpdateEnkelvoudigInformatieObjectCommandHandler CreateHandler()
    {
        return new UpdateEnkelvoudigInformatieObjectCommandHandler(
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

    private void DumpVersies()
    {
        foreach (var versie in _mockDbContext.EnkelvoudigInformatieObjectVersies.OrderByDescending(d => d.Versie))
        {
            Debug.WriteLine($"{versie.Versie} {versie.Bestandsnaam, -45} {versie.Bestandsomvang, 9} {versie.Inhoud}");
        }
    }
}
