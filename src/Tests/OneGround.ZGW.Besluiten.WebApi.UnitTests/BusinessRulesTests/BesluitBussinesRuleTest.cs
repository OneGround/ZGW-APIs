using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.BusinessRulesTests;

public class BesluitBusinessRuleTest
{
    [Fact]
    public async Task Add_With_Identifier_And_Organisatie_Already_Used_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "500",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, true, true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("identificatie", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.IdentificationNotUnique);
    }

    [Fact]
    public async Task Add_With_Unique_Identifier_And_Organisatie_Should_Validate()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "54445",
            VerantwoordelijkeOrganisatie = "500",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, true, true, errors);

        Assert.True(valid, "Validated expected");
        Assert.True(errors.Count == 0, "No validaton errors expected");
    }

    [Fact]
    public async Task Update_With_Identifier_And_Organisatie_Already_Used_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var actualBesluit = mockDbContext.Besluiten.Single(b => b.Id == new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"));
        var updateBesluit = new Besluit
        {
            Id = new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "500",
            BesluitType = "aaa",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(actualBesluit, updateBesluit, true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("identificatie", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.UpdateNotAllowed);
    }

    [Fact]
    public async Task Update_With_Changed_BesluitType_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var actualBesluit = mockDbContext.Besluiten.Single(b => b.Id == new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"));
        var updateBesluit = new Besluit
        {
            Id = new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"),
            Identificatie = "55555",
            VerantwoordelijkeOrganisatie = "500",
            BesluitType = "ddd",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(actualBesluit, updateBesluit, true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("besluittype", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.UpdateNotAllowed);
    }

    [Fact]
    public async Task Update_With_Unique_Identifier_And_Organisatie_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var actualBesluit = mockDbContext.Besluiten.Single(b => b.Id == new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"));
        var updateBesluit = new Besluit
        {
            Id = new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "899",
            BesluitType = "aaa",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(actualBesluit, updateBesluit, true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("identificatie", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.UpdateNotAllowed);
    }

    [Fact]
    public async Task Add_With_Datum_In_Future_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "11111",
            VerantwoordelijkeOrganisatie = "500",
            Datum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, true, true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("datum", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.Invalid);
    }

    [Fact]
    public async Task Add_With_Concept_BesluitType_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m => m.GetBesluitTypeByUrlAsync("http://catalogi.user.local:5011/api/v1/besluittypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"))
            .Returns(Task.FromResult(new ServiceAgentResponse<BesluitTypeResponseDto>(response: new BesluitTypeResponseDto { Concept = true })));

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "277",
            BesluitType = "http://catalogi.user.local:5011/api/v1/besluittypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, ignoreBesluitTypeValidation: false, ignoreZaakValidation: true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("besluittype", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.NotPublished);
    }

    [Fact]
    public async Task Add_With_Non_Existing_BesluitType_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m => m.GetBesluitTypeByUrlAsync("http://catalogi.user.local:5011/api/v1/besluittypen/99999999-5614-430f-ab84-127d7ad1ff0c"))
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<BesluitTypeResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.NotFound,
                            Title = "Not found.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "277",
            BesluitType = "http://catalogi.user.local:5011/api/v1/besluittypen/99999999-5614-430f-ab84-127d7ad1ff0c",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, ignoreBesluitTypeValidation: false, ignoreZaakValidation: true, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("besluittype", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.BadUrl);
    }

    [Fact]
    public async Task Add_With_Non_Concept_BesluitType_Should_Validate()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m => m.GetBesluitTypeByUrlAsync("http://catalogi.user.local:5011/api/v1/besluittypen/47078cf1-5614-430f-ab84-127d7ad1ff0c"))
            .Returns(Task.FromResult(new ServiceAgentResponse<BesluitTypeResponseDto>(response: new BesluitTypeResponseDto { Concept = false })));

        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "277",
            BesluitType = "http://catalogi.user.local:5011/api/v1/besluittypen/47078cf1-5614-430f-ab84-127d7ad1ff0c",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, ignoreBesluitTypeValidation: false, ignoreZaakValidation: true, errors);

        Assert.True(valid, "Validated expected");
        Assert.True(errors.Count == 0, "No validaton errors expected");
    }

    [Fact]
    public async Task Add_With_Non_Existing_Zaak_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        mockZakenServiceAgent
            .Setup(m => m.GetZaakByUrlAsync("http://zaken.user.local:5005/api/v1/zaken/99999999-5614-430f-ab84-127d7ad1ff0c"))
            .Returns(
                Task.FromResult(
                    new ServiceAgentResponse<ZaakResponseDto>(
                        new ErrorResponse
                        {
                            Status = (int)HttpStatusCode.NotFound,
                            Title = "Not found.",
                            Code = ErrorCode.BadUrl,
                        }
                    )
                )
            );

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "277",
            Zaak = "http://zaken.user.local:5005/api/v1/zaken/99999999-5614-430f-ab84-127d7ad1ff0c",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, ignoreBesluitTypeValidation: true, ignoreZaakValidation: false, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("zaak", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.BadUrl);
    }

    [Fact]
    public async Task Add_With_Valid_Zaak_Should_Validate()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m => m.GetZaakTypeByUrlAsync(It.IsAny<string>()))
            .Returns(
                Task.FromResult(new ServiceAgentResponse<ZaakTypeResponseDto>(response: new ZaakTypeResponseDto { BesluitTypen = ["MyZaakType"] }))
            );

        mockZakenServiceAgent
            .Setup(m => m.GetZaakByUrlAsync("http://zaken.user.local:5005/api/v1/zaken/47078cf1-5614-430f-ab84-127d7ad1ff0c"))
            .Returns(Task.FromResult(new ServiceAgentResponse<ZaakResponseDto>(response: new ZaakResponseDto { })));

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "277",
            Zaak = "http://zaken.user.local:5005/api/v1/zaken/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            BesluitType = "MyZaakType",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, ignoreBesluitTypeValidation: true, ignoreZaakValidation: false, errors);

        Assert.True(valid, "Validated expected");
        Assert.True(errors.Count == 0, "No validaton errors expected");
    }

    [Fact]
    public async Task Add_With_Invalid_Zaak_Should_Give_Error()
    {
        var mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        var mockZakenServiceAgent = new Mock<IZakenServiceAgent>();

        mockCatalogiServiceAgent
            .Setup(m => m.GetZaakTypeByUrlAsync(It.IsAny<string>()))
            .Returns(
                Task.FromResult(new ServiceAgentResponse<ZaakTypeResponseDto>(response: new ZaakTypeResponseDto { BesluitTypen = ["MyZaakType"] }))
            );

        mockZakenServiceAgent
            .Setup(m => m.GetZaakByUrlAsync("http://zaken.user.local:5005/api/v1/zaken/47078cf1-5614-430f-ab84-127d7ad1ff0c"))
            .Returns(Task.FromResult(new ServiceAgentResponse<ZaakResponseDto>(response: new ZaakResponseDto { })));

        var mockDbContext = new BrcDbContext(await GetMockedBrcDbContext());

        var svc = new BesluitBusinessRuleService(mockDbContext, mockCatalogiServiceAgent.Object, mockZakenServiceAgent.Object);

        var addBesluit = new Besluit
        {
            Id = new Guid("0c55fd91-5efd-40f7-8024-0f1763ac9177"),
            Identificatie = "67122",
            VerantwoordelijkeOrganisatie = "277",
            Zaak = "http://zaken.user.local:5005/api/v1/zaken/47078cf1-5614-430f-ab84-127d7ad1ff0c",
            BesluitType = "WrongZaakType",
        };

        var errors = new List<ValidationError>();

        var valid = await svc.ValidateAsync(addBesluit, ignoreBesluitTypeValidation: true, ignoreZaakValidation: false, errors);

        Assert.False(valid, "Validaton failed expected");
        Assert.True(errors.Count == 1, "Validaton error message expected");
        Assert.Equal("nonFieldErrors", errors[0].Name);
        Assert.True(errors[0].Code == ErrorCode.ZaakTypeMismatch);
    }

    private static async Task<DbContextOptions<BrcDbContext>> GetMockedBrcDbContext()
    {
        var options = new DbContextOptionsBuilder<BrcDbContext>().UseInMemoryDatabase(databaseName: $"brc-{Guid.NewGuid()}").Options;

        using (var context = new BrcDbContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Besluiten.Add(
                new Besluit
                {
                    Id = new Guid("10640fd5-e0ea-498b-be09-f9894f3caf68"),
                    Identificatie = "55555",
                    VerantwoordelijkeOrganisatie = "500",
                    BesluitType = "aaa",
                    Owner = "000000000",
                }
            );
            context.Besluiten.Add(
                new Besluit
                {
                    Id = new Guid("6eed92a0-20f8-427d-90dd-60c2e110f68b"),
                    Identificatie = "67122",
                    VerantwoordelijkeOrganisatie = "500",
                    BesluitType = "bbb",
                    Owner = "000000000",
                }
            );

            await context.SaveChangesAsync();
        }

        return options;
    }
}
