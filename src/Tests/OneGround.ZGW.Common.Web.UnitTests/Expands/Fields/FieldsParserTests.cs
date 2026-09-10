using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands.Fields;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands.Fields;

public class FieldsParserTests
{
    private static JToken Json(string json) => JToken.Parse(json);

    // ---- Afwezige / verkeerde top-level vorm ----

    [Fact]
    public void ParseAndValidate_Null_ReturnsAllEmptyNoError()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(null);

        Assert.Null(selection);
        Assert.Empty(expandPaths);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("\"uuid\"")]
    [InlineData("123")]
    public void ParseAndValidate_NotAnArray_ReturnsError(string json)
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json(json));

        Assert.Null(selection);
        Assert.Empty(expandPaths);
        Assert.Equal("Het 'fields' veld moet een JSON array zijn.", error);
    }

    // ---- Scalaire velden ----

    [Fact]
    public void ParseAndValidate_ScalarArray_PopulatesScalarFields()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""["uuid","identificatie"]"""));

        Assert.Null(error);
        Assert.Empty(expandPaths);
        Assert.NotNull(selection);
        Assert.Equal(new HashSet<string> { "uuid", "identificatie" }, selection!.ScalarFields);
        Assert.False(selection.IncludeAllScalars);
        Assert.Empty(selection.Entities);
    }

    [Fact]
    public void ParseAndValidate_Wildcard_SetsIncludeAllScalars()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json("""["uuid","*"]"""));

        Assert.Null(error);
        Assert.True(selection!.IncludeAllScalars);
        Assert.Contains("uuid", selection.ScalarFields);
        Assert.DoesNotContain("*", selection.ScalarFields);
    }

    [Fact]
    public void ParseAndValidate_EmptyArray_ReturnsEmptySelection()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("[]"));

        Assert.Null(error);
        Assert.NotNull(selection);
        Assert.Empty(selection!.ScalarFields);
        Assert.Empty(selection.Entities);
        Assert.Empty(expandPaths);
    }

    // ---- Geneste sub-entiteiten ----

    [Fact]
    public void ParseAndValidate_NestedEntity_CollectsExpandPath()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""["uuid",{"zaaktype":["naam"]}]"""));

        Assert.Null(error);
        Assert.Contains("uuid", selection!.ScalarFields);
        Assert.True(selection.Entities.ContainsKey("zaaktype"));
        Assert.Contains("naam", selection.Entities["zaaktype"].ScalarFields);
        Assert.Equal(["zaaktype"], expandPaths);
    }

    [Fact]
    public void ParseAndValidate_DeepNesting_CollectsDottedExpandPaths()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""[{"zaaktype":[{"detail":["waarde"]}]}]"""));

        Assert.Null(error);
        Assert.True(selection!.Entities["zaaktype"].Entities.ContainsKey("detail"));
        Assert.Equal(new HashSet<string> { "zaaktype", "zaaktype.detail" }, new HashSet<string>(expandPaths));
    }

    [Fact]
    public void ParseAndValidate_DuplicateEntityKeys_AreMerged()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""[{"zaaktype":["naam"]},{"zaaktype":["code"]}]"""));

        Assert.Null(error);
        var child = selection!.Entities["zaaktype"];
        Assert.Equal(new HashSet<string> { "naam", "code" }, child.ScalarFields);
        // Na merge één entiteit → één expand-pad.
        Assert.Equal(["zaaktype"], expandPaths);
    }

    // ---- Inline geneste objecten (gepunte syntax) ----

    [Fact]
    public void ParseAndValidate_DottedPath_PopulatesNestedObject_NoExpandPath()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""["uuid","verlenging.reden"]"""));

        Assert.Null(error);
        Assert.Contains("uuid", selection!.ScalarFields);
        Assert.True(selection.NestedObjects.ContainsKey("verlenging"));
        Assert.Contains("reden", selection.NestedObjects["verlenging"].ScalarFields);
        // Inline geneste objecten zijn géén expand-paden.
        Assert.Empty(expandPaths);
        Assert.Empty(selection.Entities);
    }

    [Fact]
    public void ParseAndValidate_DottedWildcard_SetsIncludeAllScalarsOnNested()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json("""["verlenging.*"]"""));

        Assert.Null(error);
        Assert.True(selection!.NestedObjects["verlenging"].IncludeAllScalars);
        Assert.Empty(selection.NestedObjects["verlenging"].ScalarFields);
    }

    [Fact]
    public void ParseAndValidate_DeepDottedPath_NestsRecursively()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""["a.b.c"]"""));

        Assert.Null(error);
        Assert.Empty(expandPaths);
        var b = selection!.NestedObjects["a"].NestedObjects["b"];
        Assert.Contains("c", b.ScalarFields);
    }

    [Fact]
    public void ParseAndValidate_MultipleDottedPathsSameHead_AreMerged()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json("""["verlenging.reden","verlenging.duur"]"""));

        Assert.Null(error);
        Assert.Equal(new HashSet<string> { "reden", "duur" }, selection!.NestedObjects["verlenging"].ScalarFields);
    }

    [Fact]
    public void ParseAndValidate_DottedPathInsideEntity_NestsUnderEntity()
    {
        // Binnen een expand mag inline geneste field-selectie staan (betrokkeneIdentificatie.inpBsn).
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""[{"rollen":["betrokkeneIdentificatie.inpBsn"]}]"""));

        Assert.Null(error);
        var rollen = selection!.Entities["rollen"];
        Assert.True(rollen.NestedObjects.ContainsKey("betrokkeneIdentificatie"));
        Assert.Contains("inpBsn", rollen.NestedObjects["betrokkeneIdentificatie"].ScalarFields);
        // Alleen "rollen" is een expand-pad; het inline object niet.
        Assert.Equal(["rollen"], expandPaths);
    }

    [Fact]
    public void ParseAndValidate_NestedObjectsInsideDuplicateEntities_AreMerged()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            Json("""[{"rollen":["betrokkeneIdentificatie.inpBsn"]},{"rollen":["betrokkeneIdentificatie.voornamen"]}]""")
        );

        Assert.Null(error);
        var betrokkene = selection!.Entities["rollen"].NestedObjects["betrokkeneIdentificatie"];
        Assert.Equal(new HashSet<string> { "inpBsn", "voornamen" }, betrokkene.ScalarFields);
    }

    [Theory]
    [InlineData("\"verlenging.\"")]
    [InlineData("\".reden\"")]
    [InlineData("\"a..b\"")]
    public void ParseAndValidate_EmptyPathSegment_ReturnsError(string json)
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json($"[{json}]"));

        Assert.Null(selection);
        Assert.StartsWith("Ongeldig veldpad", error);
    }

    [Fact]
    public void ParseAndValidate_PathExceedingMaxDepth_ReturnsError()
    {
        // Te diep gepunt pad wordt geweigerd (bescherming tegen onbegrensde nesting).
        var deepPath = string.Join(".", Enumerable.Repeat("a", 25));
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json($"""["{deepPath}"]"""));

        Assert.Null(selection);
        Assert.StartsWith("Ongeldig veldpad", error);
    }

    [Fact]
    public void ParseAndValidate_ObjectNestingExceedingMaxDepth_ReturnsError()
    {
        // Te diep geneste { "naam": [...] } objecten worden geweigerd (bescherming tegen onbegrensde
        // recursie in ParseArray zelf, los van de lengte van een los gepunt veldpad).
        var json = "[\"x\"]";
        for (var i = 0; i < 25; i++)
            json = $"[{{\"a\":{json}}}]";

        var (selection, _, error) = FieldsParser.ParseAndValidate(Json(json));

        Assert.Null(selection);
        Assert.StartsWith("Te diep geneste", error);
    }

    // ---- Foutgevallen ----

    [Fact]
    public void ParseAndValidate_NestedValueNotArray_ReturnsError()
    {
        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(Json("""[{"zaaktype":"naam"}]"""));

        Assert.Null(selection);
        Assert.Empty(expandPaths);
        Assert.Equal("De waarde van 'zaaktype' in 'fields' moet een JSON array zijn.", error);
    }

    [Fact]
    public void ParseAndValidate_InvalidElementAtTopLevel_ReturnsErrorMentioningFields()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json("[123]"));

        Assert.Null(selection);
        Assert.Equal("Elk element in 'fields' moet een veldnaam (string) of een sub-entiteit (object) zijn.", error);
    }

    [Fact]
    public void ParseAndValidate_InvalidElementNested_ReturnsErrorMentioningPrefix()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(Json("""[{"zaaktype":[123]}]"""));

        Assert.Null(selection);
        Assert.Equal("Elk element in 'zaaktype' moet een veldnaam (string) of een sub-entiteit (object) zijn.", error);
    }
}
