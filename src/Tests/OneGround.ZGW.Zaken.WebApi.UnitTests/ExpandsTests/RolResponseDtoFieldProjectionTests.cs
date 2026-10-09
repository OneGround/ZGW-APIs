using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

// Discriminates the exact production crash this was found from: GET /zaken/_zoek with a "fields"
// selection that reaches a ROL subtype (e.g. via a wildcard "rollen" entity selection) used to throw
// ArgumentNullException from FieldProjector, because BetrokkeneIdentificatie only carries
// [JsonProperty(Order = 1000)] -- no explicit name -- and FieldProjector didn't replicate
// Newtonsoft's own camelCase-member-name fallback for that case (see JsonPropertyNames).
public class RolResponseDtoFieldProjectionTests
{
    [Fact]
    public void Project_MedewerkerRolResponseDto_IncludeAllScalars_DoesNotThrowAndCamelCasesBetrokkeneIdentificatie()
    {
        var rol = new MedewerkerRolResponseDto
        {
            Url = "https://zrc.test/rollen/1",
            BetrokkeneIdentificatie = new() { Identificatie = "medewerker-123" },
        };
        var selection = new FieldSelection { IncludeAllScalars = true };

        var result = FieldProjector.Project(rol, selection);

        Assert.Equal("https://zrc.test/rollen/1", result["url"]);
        var betrokkeneIdentificatie = Assert.IsType<OneGround.ZGW.Zaken.Contracts.v1.MedewerkerZaakRolDto>(result["betrokkeneIdentificatie"]);
        Assert.Equal("medewerker-123", betrokkeneIdentificatie.Identificatie);
    }
}
