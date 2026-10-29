using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters;
using Xunit;
using V15ZaakObjectRequests = OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using V15ZaakRol = OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using V1ZaakObjectRequests = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using V1ZaakRol = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

#pragma warning disable CS0618 // Note: the misspelled members are deprecated, but they must keep working: that is what these tests pin

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// The two typo fixes of the VNG standard that must not break existing clients: the misspelled enum value of innRechtsvorm (#2581) and the
/// misspelled oaoIdentificatie of the adresAanduidingGrp of a terrein_gebouwd_object zaakobject (#2576). Both stay accepted and returned; the
/// correct spelling is added next to them.
/// </summary>
public class GemmaTypoFixesContractTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();

    public GemmaTypoFixesContractTests()
    {
        // Mirrors the api wiring so error names resolve to their [JsonProperty] names.
        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Default;
    }

    public void Dispose() => _host.Dispose();

    // ---- #2581: innRechtsvorm

    [Fact]
    public void InnRechtsvorm_StoredValuesDoNotShift()
    {
        // Note: stored as smallint, so a shift of any member silently changes what is in the database. The correct spelling goes last and the
        // misspelled one keeps its place. Every member is pinned, so adding, removing or reordering one fails here.
        var expected = new[]
        {
            "besloten_vennootschap",
            "cooperatie_europees_economische_samenwerking",
            "europese_cooperatieve_venootschap",
            "europese_naamloze_vennootschap",
            "kerkelijke_organisatie",
            "naamloze_vennootschap",
            "onderlinge_waarborg_maatschappij",
            "overig_privaatrechtelijke_rechtspersoon",
            "stichting",
            "vereniging",
            "vereniging_van_eigenaars",
            "publiekrechtelijke_rechtspersoon",
            "vennootschap_onder_firma",
            "maatschap",
            "rederij",
            "commanditaire_vennootschap",
            "kapitaalvennootschap_binnen_eer",
            "overige_buitenlandse_rechtspersoon_vennootschap",
            "kapitaalvennootschap_buiten_eer",
            "europese_cooperatieve_vennootschap",
        };

        for (var ordinal = 0; ordinal < expected.Length; ordinal++)
        {
            Assert.Equal(expected[ordinal], Enum.GetName(typeof(InnRechtsvorm), (short)ordinal));
        }

        Assert.Equal(expected.Length, Enum.GetValues<InnRechtsvorm>().Length);
    }

    [Theory]
    [InlineData("europese_cooperatieve_venootschap", 2)]
    [InlineData("europese_cooperatieve_vennootschap", 19)]
    public void InnRechtsvorm_BothSpellings_AreMappedFromARequest(string innRechtsvorm, int expectedStoredValue)
    {
        var source = new NietNatuurlijkPersoonZaakRolDto { InnRechtsvorm = innRechtsvorm };

        var result = _host.Mapper.Map<NietNatuurlijkPersoonZaakRol>(source);

        Assert.Equal(expectedStoredValue, (short)result.InnRechtsvorm!.Value);
    }

    [Theory]
    [InlineData("europese_cooperatieve_venootschap")]
    [InlineData("europese_cooperatieve_vennootschap")]
    public void InnRechtsvorm_BothSpellings_AreReturnedAsTheyWereStored(string innRechtsvorm)
    {
        // Note: not normalized to the correct spelling, or a client that compares with what it sent would break
        var source = new NietNatuurlijkPersoonZaakRol { InnRechtsvorm = Enum.Parse<InnRechtsvorm>(innRechtsvorm) };

        var result = _host.Mapper.Map<NietNatuurlijkPersoonZaakRolDto>(source);

        Assert.Equal(innRechtsvorm, result.InnRechtsvorm);
    }

    [Theory]
    [InlineData("europese_cooperatieve_venootschap", false)]
    [InlineData("europese_cooperatieve_vennootschap", false)]
    [InlineData("europese_cooperatieve_vennootschapp", true)]
    public void InnRechtsvorm_Validator_AcceptsBothSpellings(string innRechtsvorm, bool expectError)
    {
        var model = new V15ZaakRol.NietNatuurlijkPersoonZaakRolRequestDto
        {
            BetrokkeneIdentificatie = new NietNatuurlijkPersoonZaakRolDto { InnRechtsvorm = innRechtsvorm },
        };

        var result = new Web.Validators.v1._5.ZaakRol.NietNatuurlijkPersoonZaakRolRequestValidator().TestValidate(model);

        if (expectError)
        {
            result.ShouldHaveValidationErrorFor("betrokkeneIdentificatie.innRechtsvorm");
        }
        else
        {
            result.ShouldNotHaveValidationErrorFor("betrokkeneIdentificatie.innRechtsvorm");
        }
    }

    [Theory]
    [InlineData("europese_cooperatieve_venootschap", false)]
    [InlineData("europese_cooperatieve_vennootschap", false)]
    [InlineData("europese_cooperatieve_vennootschapp", true)]
    public void InnRechtsvorm_ValidatorOfV1_AcceptsBothSpellings(string innRechtsvorm, bool expectError)
    {
        var model = new V1ZaakRol.NietNatuurlijkPersoonZaakRolRequestDto
        {
            BetrokkeneIdentificatie = new NietNatuurlijkPersoonZaakRolDto { InnRechtsvorm = innRechtsvorm },
        };

        var result = new Web.Validators.v1.ZaakRol.NietNatuurlijkPersoonZaakRolRequestValidator().TestValidate(model);

        if (expectError)
        {
            result.ShouldHaveValidationErrorFor("betrokkeneIdentificatie.innRechtsvorm");
        }
        else
        {
            result.ShouldNotHaveValidationErrorFor("betrokkeneIdentificatie.innRechtsvorm");
        }
    }

    // ---- #2576: adresAanduidingGrp.oaoIdentificatie -> aoaIdentificatie

    private static AdresAanduidingGrpDto Adres(string oao, string aoa) =>
        new()
        {
            OaoIdentificatie = oao,
            AoaIdentificatie = aoa,
            WplWoonplaatsNaam = "Teststad",
            GorOpenbareRuimteNaam = "Teststraat",
            AoaHuisnummer = 1,
        };

    private static TerreinGebouwdObjectZaakObjectDto Terrein(string oao, string aoa) =>
        new() { Identificatie = "ID1", AdresAanduidingGrp = Adres(oao, aoa) };

    private static TerreinGebouwdObjectZaakObject ExistingTerrein(string storedIdentificatie) =>
        new()
        {
            Id = Guid.NewGuid(),
            Identificatie = "ID1",
            AdresAanduidingGrp_OaoIdentificatie = storedIdentificatie,
            AdresAanduidingGrp_WplWoonplaatsNaam = "Teststad",
            AdresAanduidingGrp_GorOpenbareRuimteNaam = "Teststraat",
            AdresAanduidingGrp_AoaHuisnummer = 1,
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

    [Theory]
    [InlineData("OAO", null, "OAO")]
    [InlineData(null, "AOA", "AOA")]
    [InlineData("OAO", "AOA", "AOA")]
    public void Request_V1_ResolvesTheIdentificatie_AoaWinsWhenSet(string oao, string aoa, string expected)
    {
        var source = new V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein(oao, aoa) };

        var result = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(source);

        Assert.Equal(expected, result.AdresAanduidingGrp_OaoIdentificatie);
    }

    [Theory]
    [InlineData("OAO", null, "OAO")]
    [InlineData(null, "AOA", "AOA")]
    [InlineData("OAO", "AOA", "AOA")]
    public void Request_V15_ResolvesTheIdentificatie_AoaWinsWhenSet(string oao, string aoa, string expected)
    {
        var source = new V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein(oao, aoa) };

        var result = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(source);

        Assert.Equal(expected, result.AdresAanduidingGrp_OaoIdentificatie);
    }

    [Fact]
    public void Response_ReturnsTheStoredIdentificatieUnderBothNames()
    {
        var result = _host.Mapper.Map<TerreinGebouwdObjectZaakObjectDto>(ExistingTerrein("STORED"));

        Assert.Equal("STORED", result.AdresAanduidingGrp.OaoIdentificatie);
        Assert.Equal("STORED", result.AdresAanduidingGrp.AoaIdentificatie);
    }

    [Fact]
    public void Response_Serializes_BothNames()
    {
        var result = _host.Mapper.Map<TerreinGebouwdObjectZaakObjectDto>(ExistingTerrein("STORED"));

        var json = JObject.FromObject(result);

        Assert.Equal("STORED", (string)json["adresAanduidingGrp"]["oaoIdentificatie"]);
        Assert.Equal("STORED", (string)json["adresAanduidingGrp"]["aoaIdentificatie"]);
    }

    private TRequest PatchMerge<TRequest>(string stored, string adresAanduidingGrpPatch) =>
        new RequestMerger(_host.Mapper).MergePartialUpdateToObjectRequest<TRequest, TerreinGebouwdObjectZaakObject>(
            ExistingTerrein(stored),
            JObject.Parse("{\"objectIdentificatie\":{\"adresAanduidingGrp\":{" + adresAanduidingGrpPatch + "}}}")
        );

    // Note: the merge base holds the stored value under the deprecated name only. With both names filled, a payload with only one of them would
    // leave the other holding the old value, and aoaIdentificatie would win over a changed oaoIdentificatie.
    [Theory]
    [InlineData("", "STORED")]
    [InlineData("\"oaoIdentificatie\":\"NEW\"", "NEW")]
    [InlineData("\"aoaIdentificatie\":\"NEW\"", "NEW")]
    [InlineData("\"oaoIdentificatie\":\"OLD\",\"aoaIdentificatie\":\"NEW\"", "NEW")]
    public void Patch_V1_TheNameThePayloadUsesWinsOverTheStoredValue(string patch, string expected)
    {
        var merged = PatchMerge<V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto>("STORED", patch);

        var result = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(merged);

        Assert.Equal(expected, result.AdresAanduidingGrp_OaoIdentificatie);
    }

    [Theory]
    [InlineData("", "STORED")]
    [InlineData("\"oaoIdentificatie\":\"NEW\"", "NEW")]
    [InlineData("\"aoaIdentificatie\":\"NEW\"", "NEW")]
    [InlineData("\"oaoIdentificatie\":\"OLD\",\"aoaIdentificatie\":\"NEW\"", "NEW")]
    public void Patch_V15_TheNameThePayloadUsesWinsOverTheStoredValue(string patch, string expected)
    {
        var merged = PatchMerge<V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto>("STORED", patch);

        var result = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(merged);

        Assert.Equal(expected, result.AdresAanduidingGrp_OaoIdentificatie);
    }

    private static IEnumerable<IValidator<TerreinGebouwdObjectZaakObjectDto>> TerreinValidators()
    {
        yield return new Web.Validators.v1.ZaakObject.TerreinGebouwdObjectZaakObjectDtoValidator();
        yield return new Web.Validators.v1._5.ZaakObject.TerreinGebouwdObjectZaakObjectDtoValidator();
    }

    [Theory]
    [InlineData("OAO", null)]
    [InlineData(null, "AOA")]
    [InlineData("OAO", "AOA")]
    public void Validator_OneOfTheTwoNamesIsEnough(string oao, string aoa)
    {
        foreach (var validator in TerreinValidators())
        {
            var result = validator.TestValidate(Terrein(oao, aoa));

            result.ShouldNotHaveValidationErrorFor("adresAanduidingGrp.oaoIdentificatie");
            result.ShouldNotHaveValidationErrorFor("adresAanduidingGrp.aoaIdentificatie");
        }
    }

    [Fact]
    public void Validator_NeitherName_ReportsBothNames()
    {
        foreach (var validator in TerreinValidators())
        {
            var result = validator.TestValidate(Terrein(null, null));

            result.ShouldHaveValidationErrorFor("adresAanduidingGrp.oaoIdentificatie");
            result.ShouldHaveValidationErrorFor("adresAanduidingGrp.aoaIdentificatie");
        }
    }

    [Theory]
    [InlineData("", null, "adresAanduidingGrp.oaoIdentificatie")]
    [InlineData("   ", null, "adresAanduidingGrp.oaoIdentificatie")]
    [InlineData("", "AOA", "adresAanduidingGrp.oaoIdentificatie")] // Note: as before, also next to the other name
    [InlineData("   ", "AOA", "adresAanduidingGrp.oaoIdentificatie")]
    [InlineData("OAO", "", "adresAanduidingGrp.aoaIdentificatie")]
    [InlineData("OAO", "   ", "adresAanduidingGrp.aoaIdentificatie")]
    public void Validator_AnEmptyName_IsRejected(string oao, string aoa, string expectedErrorFor)
    {
        foreach (var validator in TerreinValidators())
        {
            validator.TestValidate(Terrein(oao, aoa)).ShouldHaveValidationErrorFor(expectedErrorFor);
        }
    }

    [Fact]
    public void Validator_OaoIdentificatie_IsLimitedTo100Characters()
    {
        foreach (var validator in TerreinValidators())
        {
            validator.TestValidate(Terrein(new string('x', 101), null)).ShouldHaveValidationErrorFor("adresAanduidingGrp.oaoIdentificatie");
        }
    }

    [Fact]
    public void Validator_AoaIdentificatie_IsLimitedTo100Characters()
    {
        foreach (var validator in TerreinValidators())
        {
            validator.TestValidate(Terrein(null, new string('x', 101))).ShouldHaveValidationErrorFor("adresAanduidingGrp.aoaIdentificatie");
        }
    }

    // ---- Both names given and different: who wins

    private static ZaakObject AsTerreinZaakObject(TerreinGebouwdObjectZaakObject terrein) =>
        new() { ObjectType = ObjectType.terrein_gebouwd_object, TerreinGebouwdObject = terrein };

    private static string Update(string stored, TerreinGebouwdObjectZaakObject requested)
    {
        var source = AsTerreinZaakObject(ExistingTerrein(stored));

        new ZaakObjectUpdater().Update(AsTerreinZaakObject(requested), source, version: 1.5M);

        return source.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatie;
    }

    private TerreinGebouwdObjectZaakObject MapV1(string oao, string aoa) =>
        _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(
            new V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein(oao, aoa) }
        );

    private TerreinGebouwdObjectZaakObject MapV15(string oao, string aoa) =>
        _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(
            new V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein(oao, aoa) }
        );

    // Note: the stored value is "S". A client that does GET, changes one name and sends the whole body back still sends the old value under the other.
    [Theory]
    [InlineData("NEW", "S", "NEW")] // changed only the deprecated name
    [InlineData("S", "NEW", "NEW")] // changed only the correct name
    [InlineData("X", "Y", "Y")] // changed both, differently: aoaIdentificatie wins
    [InlineData("S", "S", "S")]
    [InlineData("NEW", "NEW", "NEW")]
    [InlineData("NEW", null, "NEW")]
    [InlineData(null, "NEW", "NEW")]
    [InlineData("", "S", "S")] // a blank name is not a change
    [InlineData("   ", "S", "S")]
    [InlineData("", "NEW", "NEW")]
    public void Update_TheNameThatDiffersFromTheStoredValueWins(string oao, string aoa, string expected)
    {
        Assert.Equal(expected, Update("S", MapV1(oao, aoa)));
        Assert.Equal(expected, Update("S", MapV15(oao, aoa)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Update_WithoutAUsableStoredValue_AndTwoDifferentNames_AoaWins(string stored)
    {
        Assert.Equal("Y", Update(stored, MapV1("X", "Y")));
        Assert.Equal("Y", Update(stored, MapV15("X", "Y")));
    }

    // Note: POST and PUT of a whole zaakobject map the request through the nested ZaakObject mapping instead of the terrein_gebouwd_object one. Both
    // must fill what was sent per name, or the update silently falls back to aoaIdentificatie always winning.
    [Fact]
    public void TheWholeZaakObjectRequestOfV1_FillsWhatWasSentPerName()
    {
        var mapped = _host.Mapper.Map<ZaakObject>(
            new V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein("OAO", "AOA") }
        );

        Assert.Equal("OAO", mapped.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatieAsSent);
        Assert.Equal("AOA", mapped.TerreinGebouwdObject.AdresAanduidingGrp_AoaIdentificatieAsSent);
        Assert.Equal("AOA", mapped.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatie);
    }

    [Fact]
    public void TheWholeZaakObjectRequestOfV15_FillsWhatWasSentPerName()
    {
        var mapped = _host.Mapper.Map<ZaakObject>(
            new V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein("OAO", "AOA") }
        );

        Assert.Equal("OAO", mapped.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatieAsSent);
        Assert.Equal("AOA", mapped.TerreinGebouwdObject.AdresAanduidingGrp_AoaIdentificatieAsSent);
        Assert.Equal("AOA", mapped.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatie);
    }

    [Fact]
    public void TheNestedDtoMapping_FillsWhatWasSentPerName()
    {
        var mapped = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(Terrein("OAO", "AOA"));

        Assert.Equal("OAO", mapped.AdresAanduidingGrp_OaoIdentificatieAsSent);
        Assert.Equal("AOA", mapped.AdresAanduidingGrp_AoaIdentificatieAsSent);
    }

    [Theory]
    [InlineData("NEW", "S")]
    [InlineData("S", "NEW")]
    public void GetThenPutOfTheWholeZaakObject_StoresTheChangedName(string oao, string aoa)
    {
        var requested = _host.Mapper.Map<ZaakObject>(
            new V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = Terrein(oao, aoa) }
        );
        var source = AsTerreinZaakObject(ExistingTerrein("S"));

        new ZaakObjectUpdater().Update(requested, source, version: 1.5M);

        Assert.Equal("NEW", source.TerreinGebouwdObject.AdresAanduidingGrp_OaoIdentificatie);
    }

    [Fact]
    public void Update_WithoutWhatWasSent_UsesTheResolvedValue()
    {
        // Note: a request object that was not mapped from a dto knows only the resolved value
        var requested = new TerreinGebouwdObjectZaakObject { AdresAanduidingGrp_OaoIdentificatie = "RESOLVED" };

        Assert.Equal("RESOLVED", Update("S", requested));
    }

    [Fact]
    public void Create_BothNamesDiffer_AoaWins()
    {
        // Note: nothing is stored yet that could tell which of the two was changed
        Assert.Equal("Y", MapV1("X", "Y").AdresAanduidingGrp_OaoIdentificatie);
        Assert.Equal("Y", MapV15("X", "Y").AdresAanduidingGrp_OaoIdentificatie);
    }

    [Fact]
    public void GetThenPutWithOnlyTheDeprecatedNameChanged_StoresTheChange()
    {
        var response = _host.Mapper.Map<TerreinGebouwdObjectZaakObjectDto>(ExistingTerrein("S"));
        response.AdresAanduidingGrp.OaoIdentificatie = "NEW";

        var asV1 = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(
            new V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = response }
        );

        Assert.Equal("NEW", Update("S", asV1));
    }

    [Fact]
    public void GetThenPutWithOnlyTheCorrectNameChanged_StoresTheChange()
    {
        var response = _host.Mapper.Map<TerreinGebouwdObjectZaakObjectDto>(ExistingTerrein("S"));
        response.AdresAanduidingGrp.AoaIdentificatie = "NEW";

        var asV15 = _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(
            new V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = response }
        );

        Assert.Equal("NEW", Update("S", asV15));
    }

    [Fact]
    public void PatchWithTheWholeGetBodyAndOnlyTheDeprecatedNameChanged_StoresTheChange()
    {
        var merged = PatchMerge<V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto>(
            "S",
            "\"oaoIdentificatie\":\"NEW\",\"aoaIdentificatie\":\"S\""
        );

        Assert.Equal("NEW", Update("S", _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(merged)));
    }

    [Fact]
    public void TheValuesAsSent_AreNotPartOfTheStoredModel()
    {
        // Note: they only carry what the client sent to the update. Mapped, they would need a migration and a column.
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase($"zrc-{Guid.NewGuid()}").Options;
        using var db = new HandlerTests.UnitTestZrcDbContext(options);

        var entityType = db.Model.FindEntityType(typeof(TerreinGebouwdObjectZaakObject));

        Assert.NotNull(entityType.FindProperty(nameof(TerreinGebouwdObjectZaakObject.AdresAanduidingGrp_OaoIdentificatie)));
        Assert.Null(entityType.FindProperty(nameof(TerreinGebouwdObjectZaakObject.AdresAanduidingGrp_OaoIdentificatieAsSent)));
        Assert.Null(entityType.FindProperty(nameof(TerreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaIdentificatieAsSent)));
    }

    // ---- null and empty

    [Fact]
    public void Request_WithoutAdresAanduidingGrp_LeavesEverythingNull()
    {
        var dto = new TerreinGebouwdObjectZaakObjectDto { Identificatie = "ID1" };
        var mapped = new[]
        {
            _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(
                new V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = dto }
            ),
            _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(
                new V15ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto { ObjectIdentificatie = dto }
            ),
        };

        foreach (var requested in mapped)
        {
            Assert.Null(requested.AdresAanduidingGrp_OaoIdentificatie);
            Assert.Null(requested.AdresAanduidingGrp_OaoIdentificatieAsSent);
            Assert.Null(requested.AdresAanduidingGrp_AoaIdentificatieAsSent);
        }
    }

    [Fact]
    public void Patch_AnExplicitNullForTheDeprecatedName_ClearsTheValue_SoTheValidatorRejectsIt()
    {
        var merged = PatchMerge<V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto>("S", "\"oaoIdentificatie\":null");

        Assert.Null(_host.Mapper.Map<TerreinGebouwdObjectZaakObject>(merged).AdresAanduidingGrp_OaoIdentificatie);
        foreach (var validator in TerreinValidators())
        {
            var result = validator.TestValidate(merged.ObjectIdentificatie);

            result.ShouldHaveValidationErrorFor("adresAanduidingGrp.oaoIdentificatie");
            result.ShouldHaveValidationErrorFor("adresAanduidingGrp.aoaIdentificatie");
        }
    }

    [Fact]
    public void Patch_AnExplicitNullForTheCorrectName_KeepsTheStoredValue()
    {
        var merged = PatchMerge<V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto>("S", "\"aoaIdentificatie\":null");

        Assert.Equal("S", Update("S", _host.Mapper.Map<TerreinGebouwdObjectZaakObject>(merged)));
    }

    [Fact]
    public void Patch_WithNothingStored_AndNothingSent_StaysNull()
    {
        var merged = PatchMerge<V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto>(null, "");

        Assert.Null(_host.Mapper.Map<TerreinGebouwdObjectZaakObject>(merged).AdresAanduidingGrp_OaoIdentificatie);
    }

    // Note: the merge base is a second variant of the response mapping (see AdaptTerreinGebouwdObjectForPatchMerge). It must differ from the response
    // in aoaIdentificatie only, or a field added to the dto later would silently be left out of a PATCH.
    [Fact]
    public void PatchMergeBase_DiffersFromTheResponse_InAoaIdentificatieOnly()
    {
        var existing = ExistingTerrein("S");
        existing.AdresAanduidingGrp_NumIdentificatie = "NUM";
        existing.AdresAanduidingGrp_AoaPostcode = "1234AB";
        existing.AdresAanduidingGrp_AoaHuisletter = "A";
        existing.AdresAanduidingGrp_AoaHuisnummertoevoeging = "bis";
        existing.AdresAanduidingGrp_OgoLocatieAanduiding = "OGO";

        var response = _host.Mapper.Map<TerreinGebouwdObjectZaakObjectDto>(existing).AdresAanduidingGrp;
        var mergeBase = new RequestMerger(_host.Mapper)
            .MergePartialUpdateToObjectRequest<V1ZaakObjectRequests.TerreinGebouwdObjectZaakObjectRequestDto, TerreinGebouwdObjectZaakObject>(
                existing,
                new JObject()
            )
            .ObjectIdentificatie.AdresAanduidingGrp;

        foreach (var property in typeof(AdresAanduidingGrpDto).GetProperties())
        {
            var expected = property.Name == nameof(AdresAanduidingGrpDto.AoaIdentificatie) ? null : property.GetValue(response);

            Assert.True(Equals(expected, property.GetValue(mergeBase)), $"{property.Name} of the merge base differs from the response");
        }
    }
}
