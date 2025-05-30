using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace Roxit.ZGW.Catalogi.WebApi.UnitTests.BusinessRulesTests;

public class ConceptBusinessRulesTest
{
    private readonly IConfiguration _mockedConfiguration;
    private readonly ILogger<ConceptBusinessRule> _mockedLogger;
    private readonly Mock<IEntityUriService> _mockedUriService;
    private readonly BesluitTypeRelationsValidator _besluitTypeRelationsValidator;

    public ConceptBusinessRulesTest()
    {
        var inMemorySettings = new Dictionary<string, string> { { "Application:IgnoreBusinessRulesZtc010AndZtc011", "false" } };

        _mockedConfiguration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        _mockedLogger = Mock.Of<ILogger<ConceptBusinessRule>>();
        _mockedUriService = new Mock<IEntityUriService>();

        _besluitTypeRelationsValidator = new BesluitTypeRelationsValidator(_mockedUriService.Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidatesIfEntityIsConcept(bool concept)
    {
        var conceptBusinessRule = new ConceptBusinessRule(_mockedConfiguration, _mockedLogger);
        var entity = new InformatieObjectType() { Concept = concept };
        var entityValidationresult = conceptBusinessRule.ValidateConcept(entity, []);

        Assert.Equal(concept, entityValidationresult);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidatesIfEntityZaakTypeIsConcept(bool concept)
    {
        var conceptBusinessRule = new ConceptBusinessRule(_mockedConfiguration, _mockedLogger);
        var entity = new StatusType { ZaakType = new ZaakType { Concept = concept } };
        var entityValidationresult = conceptBusinessRule.ValidateConceptZaakType(entity.ZaakType, []);

        Assert.Equal(concept, entityValidationresult);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidatesIfEntityRelationIsConcept(bool concept)
    {
        var conceptBusinessRule = new ConceptBusinessRule(_mockedConfiguration, _mockedLogger);
        var entity = new ZaakType { ZaakTypeBesluitTypen = [new ZaakTypeBesluitType { BesluitType = new BesluitType { Concept = concept } }] };
        var entityValidationresult = conceptBusinessRule.ValidateConceptRelation(
            entity.ZaakTypeBesluitTypen.Select(t => t.BesluitType).First(),
            [],
            version: 1.0M
        );

        Assert.Equal(concept, entityValidationresult);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateConceptRelation_ByPassing_Ztc10_And_Ztc11(bool concept)
    {
        var inMemorySettings = new Dictionary<string, string> { { "Application:IgnoreBusinessRulesZtc010AndZtc011", "true" } };

        var mockedConfiguration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var conceptBusinessRule = new ConceptBusinessRule(mockedConfiguration, _mockedLogger);
        var entity = new ZaakType { ZaakTypeBesluitTypen = [new ZaakTypeBesluitType { BesluitType = new BesluitType { Concept = concept } }] };
        var entityValidationresult = conceptBusinessRule.ValidateConceptRelation(
            entity.ZaakTypeBesluitTypen.Select(t => t.BesluitType).First(),
            [],
            version: 1.0M
        );

        Assert.True(entityValidationresult); // Due IgnoreBusinessRulesZtc010AndZtc011 set to true result will always be true
    }

    [Fact]
    public void BesluitTypeRelationsValidator_Add_New_Relation_With_Missing_Original_Relation_Should_Not_Validate()
    {
        // Setup

        var existingZaakTypeGuids = new List<Guid>
        {
            new Guid("16ee54a6-5578-16e0-014d-2ee79069bb55"),
            new Guid("835827db-e46d-4d84-a41c-ccd80c3f9e4a"),
        };

        var patchZaakTypeUrls = new List<string>
        {
            "http://catalogi.user.local:5011/api/v1/zaaktypen/835827db-e46d-4d84-a41c-ccd80c3f9e4a",
            "http://catalogi.user.local:5011/api/v1/zaaktypen/f6d279e0-3bcb-4263-9585-6448a3690a39",
            "http://catalogi.user.local:5011/api/v1/zaaktypen/2483968f-aca1-42e9-a589-6b823b0c5bea",
        };

        // Act
        bool validated = _besluitTypeRelationsValidator.Validate(existingZaakTypeGuids, patchZaakTypeUrls);

        // Assert
        Assert.False(validated);
    }

    [Fact]
    public void BesluitTypeRelationsValidator_Add_New_Relation_With_Original_Relation_Should_Validate()
    {
        // Setup

        var existingZaakTypeGuids = new List<Guid>
        {
            new Guid("f6d279e0-3bcb-4263-9585-6448a3690a39"),
            new Guid("835827db-e46d-4d84-a41c-ccd80c3f9e4a"),
        };

        var patchZaakTypeUrls = new List<string>
        {
            "http://catalogi.user.local:5011/api/v1/zaaktypen/835827db-e46d-4d84-a41c-ccd80c3f9e4a",
            "http://catalogi.user.local:5011/api/v1/zaaktypen/f6d279e0-3bcb-4263-9585-6448a3690a39",
            "http://catalogi.user.local:5011/api/v1/zaaktypen/2483968f-aca1-42e9-a589-6b823b0c5bea",
        };

        _mockedUriService.Setup(m => m.GetId(patchZaakTypeUrls[0])).Returns(Guid.Parse(patchZaakTypeUrls[0].Split('/').Last()));
        _mockedUriService.Setup(m => m.GetId(patchZaakTypeUrls[1])).Returns(Guid.Parse(patchZaakTypeUrls[1].Split('/').Last()));
        _mockedUriService.Setup(m => m.GetId(patchZaakTypeUrls[2])).Returns(Guid.Parse(patchZaakTypeUrls[2].Split('/').Last()));

        // Act
        bool validated = _besluitTypeRelationsValidator.Validate(existingZaakTypeGuids, patchZaakTypeUrls);

        // Assert
        Assert.True(validated);
    }

    [Fact]
    public void BesluitTypeRelationsValidator_Add_New_Relation_With_Original_Relation_Upper_Case_Should_Validate()
    {
        // Setup

        var existingZaakTypeGuids = new List<Guid>
        {
            new Guid("f6d279e0-3bcb-4263-9585-6448a3690a39"),
            new Guid("835827db-e46d-4d84-a41c-ccd80c3f9e4a"),
        };

        var patchZaakTypeUrls = new List<string>
        {
            "http://catalogi.user.local:5011/api/v1/zaaktypen/835827DB-E46D-4D84-A41C-CCD80C3F9E4A",
            "http://catalogi.user.local:5011/api/v1/zaaktypen/F6D279E0-3BCB-4263-9585-6448A3690A39",
            "http://catalogi.user.local:5011/api/v1/zaaktypen/2483968F-ACA1-42E9-A589-6B823B0C5BEA",
        };

        _mockedUriService.Setup(m => m.GetId(patchZaakTypeUrls[0])).Returns(Guid.Parse(patchZaakTypeUrls[0].Split('/').Last()));
        _mockedUriService.Setup(m => m.GetId(patchZaakTypeUrls[1])).Returns(Guid.Parse(patchZaakTypeUrls[1].Split('/').Last()));
        _mockedUriService.Setup(m => m.GetId(patchZaakTypeUrls[2])).Returns(Guid.Parse(patchZaakTypeUrls[2].Split('/').Last()));

        // Act
        bool validated = _besluitTypeRelationsValidator.Validate(existingZaakTypeGuids, patchZaakTypeUrls);

        // Assert
        Assert.True(validated);
    }
}
