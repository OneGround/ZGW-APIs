using OneGround.ZGW.Common.Web.Expands.Fields;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands.Fields;

public class FieldsSchemaTests
{
    // Discriminates that a [JsonProperty] without an explicit name (e.g. the real
    // BetrokkeneIdentificatie property on the Rol subtype DTOs, which relies on the ASP.NET Core
    // Newtonsoft default CamelCasePropertyNamesContractResolver to camelCase it on the wire) still
    // counts as a known scalar field -- not silently dropped, which would make FieldsValidator
    // reject a client's legitimate selection of it as "unknown".
    [Fact]
    public void ScalarsOf_PropertyWithoutExplicitJsonPropertyName_IsIncludedUnderItsCamelCasedMemberName()
    {
        var schema = new FieldsSchemaBuilder().Build();

        var scalars = schema.ScalarsOf(typeof(NamelessJsonPropertyDto));

        Assert.Contains("betrokkeneIdentificatie", scalars);
    }
}
