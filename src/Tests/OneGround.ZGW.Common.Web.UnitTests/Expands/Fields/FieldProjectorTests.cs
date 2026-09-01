using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Expands.Fields;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands.Fields;

public class FieldProjectorTests
{
    // ---- Scalaire velden ----

    [Fact]
    public void Project_SelectedScalarFields_OnlyIncludesThose()
    {
        var entity = new ParentDto { Uuid = "u1", Identificatie = "ID-1" };
        var selection = new FieldSelection { ScalarFields = { "uuid" } };

        var result = FieldProjector.Project(entity, selection);

        Assert.Equal("u1", result["uuid"]);
        Assert.False(result.ContainsKey("identificatie"));
        // Geen geneste entiteiten gevraagd → geen _expand-sleutel.
        Assert.False(result.ContainsKey("_expand"));
    }

    [Fact]
    public void Project_UnknownScalarField_IsSilentlySkipped()
    {
        var entity = new ParentDto { Uuid = "u1" };
        var selection = new FieldSelection { ScalarFields = { "uuid", "bestaatniet" } };

        var result = FieldProjector.Project(entity, selection);

        Assert.Single(result);
        Assert.Equal("u1", result["uuid"]);
    }

    [Fact]
    public void Project_IncludeAllScalars_IncludesAllJsonPropsExceptExpandAndAttributeless()
    {
        var entity = new ParentDto
        {
            Uuid = "u1",
            Identificatie = "ID-1",
            Intern = "geheim",
        };
        var selection = new FieldSelection { IncludeAllScalars = true };

        var result = FieldProjector.Project(entity, selection);

        // Alleen de twee scalairen met JsonPropertyName; niet "_expand", niet "Intern".
        Assert.Equal(2, result.Count);
        Assert.Equal("u1", result["uuid"]);
        Assert.Equal("ID-1", result["identificatie"]);
    }

    // ---- Geneste entiteiten (_expand) ----

    [Fact]
    public void Project_NestedSingleObject_ProjectsSubSelection()
    {
        var entity = new ParentDto
        {
            Uuid = "u1",
            Expand = new()
            {
                ["zaaktype"] = new ChildDto { Naam = "Vergunning", Code = "VG" },
            },
        };
        var selection = new FieldSelection
        {
            ScalarFields = { "uuid" },
            Entities = { ["zaaktype"] = new FieldSelection { ScalarFields = { "naam" } } },
        };

        var result = FieldProjector.Project(entity, selection);

        var expand = Assert.IsType<Dictionary<string, object?>>(result["_expand"]);
        var zaaktype = Assert.IsType<Dictionary<string, object?>>(expand["zaaktype"]);
        Assert.Equal("Vergunning", zaaktype["naam"]);
        Assert.False(zaaktype.ContainsKey("code"));
    }

    [Fact]
    public void Project_NestedCollection_ProjectsEachItem()
    {
        var entity = new ParentDto
        {
            Expand = new()
            {
                ["deelzaken"] = new List<ChildDto>
                {
                    new() { Naam = "A", Code = "1" },
                    new() { Naam = "B", Code = "2" },
                },
            },
        };
        var selection = new FieldSelection { Entities = { ["deelzaken"] = new FieldSelection { ScalarFields = { "naam" } } } };

        var result = FieldProjector.Project(entity, selection);

        var expand = Assert.IsType<Dictionary<string, object?>>(result["_expand"]);
        var deelzaken = Assert.IsType<List<Dictionary<string, object?>>>(expand["deelzaken"]);
        Assert.Collection(
            deelzaken,
            d =>
            {
                Assert.Equal("A", d["naam"]);
                Assert.False(d.ContainsKey("code"));
            },
            d =>
            {
                Assert.Equal("B", d["naam"]);
                Assert.False(d.ContainsKey("code"));
            }
        );
    }

    [Fact]
    public void Project_NestedEntity_MissingFromExpand_YieldsEmptyPlaceholder()
    {
        var entity = new ParentDto { Uuid = "u1", Expand = null };
        var selection = new FieldSelection { Entities = { ["zaaktype"] = new FieldSelection { ScalarFields = { "naam" } } } };

        var result = FieldProjector.Project(entity, selection);

        var expand = Assert.IsType<Dictionary<string, object?>>(result["_expand"]);
        Assert.True(expand.ContainsKey("zaaktype"));
        // Niet opgelost → kale object-placeholder, geen geprojecteerde dictionary.
        Assert.NotNull(expand["zaaktype"]);
        Assert.Equal(typeof(object), expand["zaaktype"]!.GetType());
    }

    [Fact]
    public void Project_NestedEntity_ValueIsBareObjectPlaceholder_YieldsEmptyPlaceholder()
    {
        // De expand-waarde is zelf de "leeg opgeloste" placeholder (typeof(object)).
        var entity = new ParentDto { Expand = new() { ["zaaktype"] = new object() } };
        var selection = new FieldSelection { Entities = { ["zaaktype"] = new FieldSelection { ScalarFields = { "naam" } } } };

        var result = FieldProjector.Project(entity, selection);

        var expand = Assert.IsType<Dictionary<string, object?>>(result["_expand"]);
        Assert.Equal(typeof(object), expand["zaaktype"]!.GetType());
    }

    // ---- Inline geneste objecten (in-place, niet via _expand) ----

    [Fact]
    public void Project_NestedObject_InPlace_ProjectsSubSelection()
    {
        var entity = new HostDto
        {
            Uuid = "u1",
            Verlenging = new InnerDto { A = "1", B = "2" },
        };
        var selection = new FieldSelection
        {
            ScalarFields = { "uuid" },
            NestedObjects = { ["verlenging"] = new FieldSelection { ScalarFields = { "a" } } },
        };

        var result = FieldProjector.Project(entity, selection);

        Assert.Equal("u1", result["uuid"]);
        var verlenging = Assert.IsType<Dictionary<string, object?>>(result["verlenging"]);
        Assert.Equal("1", verlenging["a"]);
        Assert.False(verlenging.ContainsKey("b"));
        // Inline object → geen _expand.
        Assert.False(result.ContainsKey("_expand"));
    }

    [Fact]
    public void Project_NestedObject_Wildcard_IncludesAllInnerScalars()
    {
        var entity = new HostDto
        {
            Verlenging = new InnerDto { A = "1", B = "2" },
        };
        var selection = new FieldSelection { NestedObjects = { ["verlenging"] = new FieldSelection { IncludeAllScalars = true } } };

        var result = FieldProjector.Project(entity, selection);

        var verlenging = Assert.IsType<Dictionary<string, object?>>(result["verlenging"]);
        Assert.Equal("1", verlenging["a"]);
        Assert.Equal("2", verlenging["b"]);
    }

    [Fact]
    public void Project_NestedObject_Null_YieldsNull()
    {
        var entity = new HostDto { Verlenging = null };
        var selection = new FieldSelection { NestedObjects = { ["verlenging"] = new FieldSelection { ScalarFields = { "a" } } } };

        var result = FieldProjector.Project(entity, selection);

        Assert.True(result.ContainsKey("verlenging"));
        Assert.Null(result["verlenging"]);
    }

    [Fact]
    public void Project_NestedObjectCollection_ProjectsEachItem()
    {
        var entity = new HostDto
        {
            Kenmerken =
            {
                new InnerDto { A = "1", B = "x" },
                new InnerDto { A = "2", B = "y" },
            },
        };
        var selection = new FieldSelection { NestedObjects = { ["kenmerken"] = new FieldSelection { ScalarFields = { "a" } } } };

        var result = FieldProjector.Project(entity, selection);

        var kenmerken = Assert.IsType<List<object?>>(result["kenmerken"]);
        Assert.Collection(
            kenmerken,
            d => Assert.Equal("1", Assert.IsType<Dictionary<string, object?>>(d)["a"]),
            d => Assert.Equal("2", Assert.IsType<Dictionary<string, object?>>(d)["a"])
        );
    }

    [Fact]
    public void Project_PolymorphicNestedObject_UsesRuntimeType()
    {
        // poly bevat een VariantOneDto; geselecteerde velden die niet op dat type zitten vallen weg.
        var entity = new HostDto { Poly = new VariantOneDto { Een = "waarde" } };
        var selection = new FieldSelection { NestedObjects = { ["poly"] = new FieldSelection { ScalarFields = { "een", "twee" } } } };

        var result = FieldProjector.Project(entity, selection);

        var poly = Assert.IsType<Dictionary<string, object?>>(result["poly"]);
        Assert.Equal("waarde", poly["een"]);
        Assert.False(poly.ContainsKey("twee")); // hoort bij VariantTwo, niet aanwezig
    }

    [Fact]
    public void Project_DeepNestedObject_ProjectsRecursively()
    {
        var entity = new HostDto { Verlenging = new InnerDto { Diep = new DeepDto { X = "diep!" } } };
        var selection = new FieldSelection
        {
            NestedObjects = { ["verlenging"] = new FieldSelection { NestedObjects = { ["diep"] = new FieldSelection { ScalarFields = { "x" } } } } },
        };

        var result = FieldProjector.Project(entity, selection);

        var verlenging = Assert.IsType<Dictionary<string, object?>>(result["verlenging"]);
        var diep = Assert.IsType<Dictionary<string, object?>>(verlenging["diep"]);
        Assert.Equal("diep!", diep["x"]);
    }

    // ---- ProjectList ----

    [Fact]
    public void ProjectList_ProjectsEachEntity()
    {
        var entities = new[]
        {
            new ParentDto { Uuid = "u1" },
            new ParentDto { Uuid = "u2" },
        };
        var selection = new FieldSelection { ScalarFields = { "uuid" } };

        var result = FieldProjector.ProjectList(entities, selection);

        Assert.Collection(result, d => Assert.Equal("u1", d["uuid"]), d => Assert.Equal("u2", d["uuid"]));
    }
}
