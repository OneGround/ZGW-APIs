using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web.MappingProfiles;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// A blank name counts as not sent: it never wins over a name that has a value, nor over the stored value.
/// </summary>
public class AdresAanduidingGrpIdentificatieTests
{
    [Theory]
    [InlineData("OAO", null, "OAO")]
    [InlineData(null, "AOA", "AOA")]
    [InlineData("OAO", "AOA", "AOA")]
    [InlineData("OAO", "", "OAO")]
    [InlineData("OAO", "   ", "OAO")]
    [InlineData("", "AOA", "AOA")]
    [InlineData("   ", "AOA", "AOA")]
    public void ResolveForCreate_IgnoresABlankName(string oao, string aoa, string expected)
    {
        Assert.Equal(expected, AdresAanduidingGrpIdentificatie.ResolveForCreate(oao, aoa));
    }

    [Theory]
    [InlineData("OAO", "", "STORED", "OAO")]
    [InlineData("OAO", "   ", "STORED", "OAO")]
    [InlineData("", "AOA", "STORED", "AOA")]
    [InlineData("", "", "STORED", "STORED")]
    [InlineData("   ", "   ", "STORED", "STORED")]
    [InlineData(null, null, "STORED", "STORED")]
    public void ResolveForUpdate_BlankNamesNeverOverrideTheStoredValue(string oao, string aoa, string stored, string expected)
    {
        var request = new TerreinGebouwdObjectZaakObject
        {
            AdresAanduidingGrp_OaoIdentificatieAsSent = oao,
            AdresAanduidingGrp_AoaIdentificatieAsSent = aoa,
            AdresAanduidingGrp_OaoIdentificatie = AdresAanduidingGrpIdentificatie.ResolveForCreate(oao, aoa),
        };

        Assert.Equal(expected, AdresAanduidingGrpIdentificatie.ResolveForUpdate(request, stored));
    }

    [Fact]
    public void ResolveForUpdate_ARequestThatWasNotMappedFromADto_UsesTheResolvedValue()
    {
        var request = new TerreinGebouwdObjectZaakObject { AdresAanduidingGrp_OaoIdentificatie = "RESOLVED" };

        Assert.Equal("RESOLVED", AdresAanduidingGrpIdentificatie.ResolveForUpdate(request, "STORED"));
    }

    [Theory]
    [InlineData("OAO", "AOA", "OAO", "AOA")] // oao is the stored one, so aoa is what changed
    [InlineData("OAO", "AOA", "AOA", "OAO")] // aoa is the stored one, so oao is what changed
    [InlineData("OAO", "AOA", "OTHER", "AOA")] // both differ from the stored one: aoa wins
    public void ResolveForUpdate_BothNamesDiffering_TheOneThatDiffersFromTheStoredValueWins(string oao, string aoa, string stored, string expected)
    {
        var request = new TerreinGebouwdObjectZaakObject
        {
            AdresAanduidingGrp_OaoIdentificatieAsSent = oao,
            AdresAanduidingGrp_AoaIdentificatieAsSent = aoa,
        };

        Assert.Equal(expected, AdresAanduidingGrpIdentificatie.ResolveForUpdate(request, stored));
    }
}
