using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands;

/// <summary>
/// DtoExpander projects a response DTO to a JObject before merging the _expand block into it. MVC
/// writes that JObject through verbatim, so whatever naming the projection applies is what the
/// client receives - the response pipeline never gets a second chance to camelCase it.
///
/// The projection therefore has to name properties the way MVC would, while keeping the converters
/// ZGWJsonSerializer carries.
/// </summary>
public class DtoExpanderTests
{
    private static readonly object AnyExpand = new { _expand = new { something = new object() } };

    private class Dto
    {
        public string UnnamedProperty { get; set; }

        [JsonProperty("explicitlyNamed")]
        public string ExplicitlyNamed { get; set; }

        [JsonProperty("alreadyCamelCase")]
        public string AlreadyCamelCase { get; set; }

        public DateOnly Date { get; set; }

        public DateOnly? NullableDate { get; set; }

        public Point Location { get; set; }
    }

    private static Dto ADto() =>
        new()
        {
            UnnamedProperty = "value",
            ExplicitlyNamed = "value",
            AlreadyCamelCase = "value",
            Date = new DateOnly(2026, 8, 26),
            NullableDate = new DateOnly(2026, 8, 26),
            Location = new Point(4.9, 52.4) { SRID = 4326 },
        };

    private static JObject Merge(object main) => (JObject)DtoExpander.Merge(main, AnyExpand);

    [Fact]
    public void UnnamedProperty_IsCamelCased()
    {
        var merged = Merge(ADto());

        Assert.True(merged.ContainsKey("unnamedProperty"));
        Assert.False(merged.ContainsKey("UnnamedProperty"));
    }

    [Fact]
    public void ExplicitlyNamedProperty_KeepsItsName()
    {
        var merged = Merge(ADto());

        Assert.True(merged.ContainsKey("explicitlyNamed"));
        Assert.True(merged.ContainsKey("alreadyCamelCase"));
    }

    [Fact]
    public void ExpandKey_KeepsItsLeadingUnderscore()
    {
        var merged = Merge(ADto());

        Assert.True(merged.ContainsKey("_expand"));
    }

    [Fact]
    public void DateOnly_StillRendersAsIsoDate()
    {
        var merged = Merge(ADto());

        Assert.Equal("2026-08-26", (string)merged["date"]);
        Assert.Equal("2026-08-26", (string)merged["nullableDate"]);
    }

    [Fact]
    public void Geometry_StillRendersAsGeoJson()
    {
        var location = Assert.IsType<JObject>(Merge(ADto())["location"]);

        Assert.Equal("Point", (string)location["type"]);
        Assert.Equal(4.9, (double)location["coordinates"][0]);
        Assert.Equal(52.4, (double)location["coordinates"][1]);
    }

    [Fact]
    public void Values_AreUnchanged()
    {
        var merged = Merge(ADto());

        Assert.Equal("value", (string)merged["unnamedProperty"]);
        Assert.Equal("value", (string)merged["explicitlyNamed"]);
        Assert.Equal("value", (string)merged["alreadyCamelCase"]);
    }

    [Fact]
    public void NullMain_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => DtoExpander.Merge(null, AnyExpand));
    }

    [Fact]
    public void NullExpand_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => DtoExpander.Merge(ADto(), null));
    }
}
