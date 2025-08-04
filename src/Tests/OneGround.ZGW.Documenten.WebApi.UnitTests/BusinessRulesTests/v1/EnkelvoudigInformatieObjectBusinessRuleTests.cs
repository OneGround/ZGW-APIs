using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;

public class EnkelvoudigInformatieObjectBusinessRuleTests
{
    [Fact]
    public async Task Add_With_Existing_Bronorganisatie_And_Identificatie_Should_Be_Invalid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject(),
            Bronorganisatie = "1234",
            Identificatie = "DOCUMENT-2020-00000002",
            Versie = 1,
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.Unique);

        Assert.NotNull(error);
        Assert.Equal("identificatie", error.Name);
    }

    [Fact]
    public async Task Add_With_Unqiue_Bronorganisatie_And_Identificatie_Should_Be_Valid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject(),
            Bronorganisatie = "2345",
            Identificatie = "DOCUMENT-2020-00000002",
            Versie = 1,
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.True(valid);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Add_With_StartDatum_And_Status_In_Bewerking_Should_Be_Invalid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject(),
            OntvangstDatum = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = Status.in_bewerking,
            Bronorganisatie = "1234",
            Identificatie = "DOCUMENT-2020-00000002",
            Versie = 1,
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.InvalidForReceived);

        Assert.NotNull(error);
        Assert.Equal("status", error.Name);
    }

    [Fact]
    public async Task Add_New_Version_With_Changed_InformatieObjectType_Should_Be_Invalid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "https://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
                Locked = true,
                Lock = "e205101a45ab4fb082a35231d63f4151",
                Owner = "813264571",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("d57b1cf4-7f37-4ebc-91d5-be9066086911");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.UpdateNotAllowed);

        Assert.NotNull(error);
        Assert.Equal("informatieobjecttype", error.Name);
    }

    [Fact]
    public async Task Add_New_Version_With_Changed_InformatieObjectType_Should_Be_Valid_If_Drc010_Is_Disabled()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(
            inMemorySettings: new Dictionary<string, string> { { "Application:IgnoreBusinessRuleDrc010", "true" } }
        );

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "https://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
                Locked = true,
                Lock = "e205101a45ab4fb082a35231d63f4151",
                Owner = "813264571",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("d57b1cf4-7f37-4ebc-91d5-be9066086911");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.True(valid);
    }

    [Fact]
    public async Task Add_New_Version_With_Existing_Definitief_EnkelvoudigInformatieObject_Should_Be_Invalid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                Locked = true,
                Lock = "e205101a45ab4fb082a35231d63f4151",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("ccbb1cf4-7f37-4ebc-1a71-be9066086955");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.UpdateNotAllowed);

        Assert.NotNull(error);
        Assert.Equal("nonFieldErrors", error.Name);
    }

    [Fact]
    public async Task Add_New_Version_With_Existing_Definitief_Should_Be_Valid_If_Drc010_Is_Disabled()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(
            inMemorySettings: new Dictionary<string, string> { { "Application:IgnoreBusinessRuleDrc010", "true" } }
        );

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                Locked = true,
                Lock = "e205101a45ab4fb082a35231d63f4151",
            },
            Bronorganisatie = "1234",
            Identificatie = "DOCUMENT-2020-00000001",
            // Change some fields within a definitive document
            Beschrijving = "Some fields may be changed for a definitive document!",
            Auteur = "Sombody else",
            Formaat = "application/pdf",
            Link = "https://somedocument/link",
            Titel = "new title",
            Taal = "EN",
            Vertrouwelijkheidaanduiding = Common.DataModel.VertrouwelijkheidAanduiding.geheim,
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("ccbb1cf4-7f37-4ebc-1a71-be9066086955");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.True(valid);
    }

    [Fact]
    public async Task Add_New_Version_Without_Lock_Should_Be_Invalid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject { Locked = false, Lock = null },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("f50ef517-8f97-4646-8b6e-02899eb80221");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.InvalidForReceived);

        Assert.NotNull(error);
        Assert.Equal("status", error.Name);
    }

    [Fact]
    public async Task Add_New_Version_With_Incorrect_Lock_Should_Be_Invalid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                Lock = "99999999999999999999999999999999",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("d57b1cf4-7f37-4ebc-91d5-be9066086911");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.IncorrectLockId);

        Assert.NotNull(error);
        Assert.Equal("nonFieldErrors", error.Name);
    }

    [Fact]
    public async Task Add_New_Version_With_Correct_Lock_Should_Be_Valid()
    {
        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService();

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                Lock = "e205101a45ab4fb082a35231d63f4151",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        var id = new Guid("d57b1cf4-7f37-4ebc-91d5-be9066086911");

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: true,
            existingEnkelvoudigInformatieObjectId: id,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.True(valid);
    }

    [Fact]
    public async Task Add_With_Unknown_InformatieObjectType_Should_Be_Invalid()
    {
        using var mockDbContext = new DrcDbContext(await GetMockedDrcDbContext());

        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m =>
                m.GetInformatieObjectTypeByUrlAsync(
                    "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<InformatieObjectTypeResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.NotFound,
                            Title = "Not found.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(mockCatalogiServiceAgent);

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: false,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.BadUrl);

        Assert.NotNull(error);
        Assert.Equal("informatieobjecttype", error.Name);
    }

    [Fact]
    public async Task Add_With_ServerError_InformatieObjectType_Should_Be_Invalid()
    {
        using var mockDbContext = new DrcDbContext(await GetMockedDrcDbContext());

        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m =>
                m.GetInformatieObjectTypeByUrlAsync(
                    "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<InformatieObjectTypeResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.InternalServerError,
                            Title = "Server error.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(mockCatalogiServiceAgent);

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: false,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.BadUrl);

        Assert.NotNull(error);
        Assert.Equal("informatieobjecttype", error.Name);
    }

    [Fact]
    public async Task Add_With_Concept_InformatieObjectType_Should_Be_Invalid()
    {
        using var mockDbContext = new DrcDbContext(await GetMockedDrcDbContext());

        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m =>
                m.GetInformatieObjectTypeByUrlAsync(
                    "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<InformatieObjectTypeResponseDto>(response: new InformatieObjectTypeResponseDto { Concept = true })
                )
            );

        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(mockCatalogiServiceAgent);

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: false,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.NotPublished);

        Assert.NotNull(error);
        Assert.Equal("informatieobjecttype", error.Name);
    }

    [Fact]
    public async Task Add_With_Ongeldige_Periode_InformatieObjectType_Should_Be_Invalid()
    {
        using var mockDbContext = new DrcDbContext(await GetMockedDrcDbContext());

        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m =>
                m.GetInformatieObjectTypeByUrlAsync(
                    "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<InformatieObjectTypeResponseDto>(
                        response: new InformatieObjectTypeResponseDto { BeginGeldigheid = "2020-01-01", EindeGeldigheid = "2020-01-31" }
                    )
                )
            );

        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(mockCatalogiServiceAgent);

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: false,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.False(valid);

        var error = errors.SingleOrDefault(e => e.Code == ErrorCode.Invalid);

        Assert.NotNull(error);
        Assert.Equal("informatieobjecttype", error.Name);
    }

    [Fact]
    public async Task Add_With_Known_And_Correct_InformatieObjectType_Should_Be_Valid()
    {
        using var mockDbContext = new DrcDbContext(await GetMockedDrcDbContext());

        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        var beginGeldigheid = DateTime.Today.AddDays(-5).ToShortDateString();
        var eindeGeldigheid = DateTime.Today.AddDays(5).ToShortDateString();
        mockCatalogiServiceAgent
            .Setup(m =>
                m.GetInformatieObjectTypeByUrlAsync(
                    "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"
                )
            )
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<InformatieObjectTypeResponseDto>(
                        response: new InformatieObjectTypeResponseDto
                        {
                            Concept = false,
                            BeginGeldigheid = beginGeldigheid,
                            EindeGeldigheid = eindeGeldigheid,
                        }
                    )
                )
            );

        var svc = await CreateEnkelvoudigInformatieObjectBusinessRuleService(mockCatalogiServiceAgent);

        var enkelvoudigInformatieObjectVersie = new EnkelvoudigInformatieObjectVersie
        {
            InformatieObject = new EnkelvoudigInformatieObject
            {
                InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            },
            Owner = "813264571",
        };

        var errors = new List<ValidationError>();

        bool valid = await svc.ValidateAsync(
            enkelvoudigInformatieObjectVersie,
            ignoreInformatieObjectTypeValidation: false,
            existingEnkelvoudigInformatieObjectId: null,
            isPartialUpdate: false,
            apiVersie: 1.0M,
            errors
        );

        Assert.True(valid);
    }

    private static async Task<DbContextOptions<DrcDbContext>> GetMockedDrcDbContext()
    {
        var options = new DbContextOptionsBuilder<DrcDbContext>().UseInMemoryDatabase(databaseName: $"drc-{Guid.NewGuid()}").Options;

        // Insert seed data into the database using one instance of the context
        using (var context = new UnitTestDrcDbContext(options))
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            context.EnkelvoudigInformatieObjecten.Add(
                new EnkelvoudigInformatieObject
                {
                    Id = new Guid("f50ef517-8f97-4646-8b6e-02899eb80221"),
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                    EnkelvoudigInformatieObjectVersies =
                    [
                        new EnkelvoudigInformatieObjectVersie
                        {
                            Bronorganisatie = "1234",
                            Identificatie = "DOCUMENT-2020-00000001",
                            Versie = 1,
                            Taal = string.Empty,
                            Owner = "813264571",
                        },
                    ],
                    Owner = "813264571",
                }
            );

            context.EnkelvoudigInformatieObjecten.Add(
                new EnkelvoudigInformatieObject
                {
                    Id = new Guid("d57b1cf4-7f37-4ebc-91d5-be9066086911"),
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                    EnkelvoudigInformatieObjectVersies =
                    [
                        new EnkelvoudigInformatieObjectVersie
                        {
                            Bronorganisatie = "1234",
                            Identificatie = "DOCUMENT-2020-00000002",
                            Versie = 1,
                            Taal = string.Empty,
                            Owner = "813264571",
                        },
                    ],
                    Owner = "813264571",
                    Locked = true,
                    Lock = "e205101a45ab4fb082a35231d63f4151",
                }
            );

            context.EnkelvoudigInformatieObjecten.Add(
                new EnkelvoudigInformatieObject
                {
                    Id = new Guid("ccbb1cf4-7f37-4ebc-1a71-be9066086955"),
                    InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                    EnkelvoudigInformatieObjectVersies =
                    [
                        new EnkelvoudigInformatieObjectVersie
                        {
                            Bronorganisatie = "1234",
                            Identificatie = "DOCUMENT-2020-00000003",
                            Versie = 1,
                            Status = Status.definitief,
                            Taal = string.Empty,
                            Owner = "813264571",
                        },
                    ],
                    Owner = "813264571",
                    Locked = true,
                    Lock = "e205101a45ab4fb082a35231d63f4151",
                }
            );

            await context.SaveChangesAsync();
        }

        return options;
    }

    private static async Task<IEnkelvoudigInformatieObjectBusinessRuleService> CreateEnkelvoudigInformatieObjectBusinessRuleService(
        Mock<ICatalogiServiceAgent> mockCatalogiServiceAgent = null,
        Dictionary<string, string> inMemorySettings = null
    )
    {
        var mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext());

        mockCatalogiServiceAgent ??= new Mock<ICatalogiServiceAgent>();

        inMemorySettings ??= new Dictionary<string, string>() { { "Application:", null } };

        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var mockLogger = new Mock<ILogger<EnkelvoudigInformatieObjectBusinessRuleService>>();

        var svc = new EnkelvoudigInformatieObjectBusinessRuleService(config, mockLogger.Object, mockDbContext, mockCatalogiServiceAgent.Object);

        return svc;
    }
}
