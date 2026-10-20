using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.TestHelper;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using Xunit;
using V15ZaakObjectRequests = OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using V15ZaakRol = OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using V1ZaakObjectRequests = OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

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
        // Note: stored as smallint. The correct spelling goes last and the misspelled one keeps its place.
        Assert.Equal(0, (short)InnRechtsvorm.besloten_vennootschap);
        Assert.Equal(2, (short)InnRechtsvorm.europese_cooperatieve_venootschap);
        Assert.Equal(3, (short)InnRechtsvorm.europese_naamloze_vennootschap);
        Assert.Equal(8, (short)InnRechtsvorm.stichting);
        Assert.Equal(18, (short)InnRechtsvorm.kapitaalvennootschap_buiten_eer);
        Assert.Equal(19, (short)InnRechtsvorm.europese_cooperatieve_vennootschap);
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
    public void Validator_NeitherName_IsRequired_UnderTheDeprecatedNameAsBefore()
    {
        foreach (var validator in TerreinValidators())
        {
            var result = validator.TestValidate(Terrein(null, null));

            result.ShouldHaveValidationErrorFor("adresAanduidingGrp.oaoIdentificatie");
        }
    }

    [Fact]
    public void Validator_AnEmptyAoaIdentificatie_IsRejected()
    {
        foreach (var validator in TerreinValidators())
        {
            var result = validator.TestValidate(Terrein("OAO", ""));

            result.ShouldHaveValidationErrorFor("adresAanduidingGrp.aoaIdentificatie");
        }
    }

    [Theory]
    [InlineData("adresAanduidingGrp.oaoIdentificatie")]
    [InlineData("adresAanduidingGrp.aoaIdentificatie")]
    public void Validator_BothNamesAreLimitedTo100Characters(string property)
    {
        var tooLong = new string('x', 101);

        foreach (var validator in TerreinValidators())
        {
            var model = property.EndsWith("oaoIdentificatie") ? Terrein(tooLong, null) : Terrein(null, tooLong);

            validator.TestValidate(model).ShouldHaveValidationErrorFor(property);
        }
    }
}
