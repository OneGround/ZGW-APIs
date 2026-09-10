using OneGround.ZGW.Common.Web.Expands.Fields;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands.Fields;

public class FieldsValidatorTests
{
    // Schema: ParentDto kent sub-entiteiten "zaaktype" (→ ChildDto) en "status" (→ ChildDto);
    // ChildDto kent sub-entiteit "detail" (→ GrandChildDto).
    private static FieldsSchema BuildSchema() =>
        new FieldsSchemaBuilder()
            .Entity<ParentDto, ChildDto>("zaaktype")
            .Entity<ParentDto, ChildDto>("status")
            .Entity<ChildDto, GrandChildDto>("detail")
            .Build();

    private static FieldsValidator<ParentDto> Validator(params string[] expandablePaths) => new(BuildSchema(), expandablePaths);

    // ---- Lege / triviale gevallen ----

    [Fact]
    public void Validate_Null_ReturnsEmpty()
    {
        Assert.Empty(Validator("zaaktype").Validate(null));
    }

    [Fact]
    public void Validate_EmptySelection_ReturnsEmpty()
    {
        Assert.Empty(Validator("zaaktype").Validate(new FieldSelection()));
    }

    // ---- Scalaire velden ----

    [Fact]
    public void Validate_ValidScalarFields_ReturnsEmpty()
    {
        var selection = new FieldSelection { ScalarFields = { "uuid", "identificatie" } };

        Assert.Empty(Validator().Validate(selection));
    }

    [Fact]
    public void Validate_UnknownScalarField_IsReported()
    {
        var selection = new FieldSelection { ScalarFields = { "uuid", "onbekend" } };

        var invalid = Validator().Validate(selection);

        Assert.Equal(["onbekend"], invalid);
    }

    [Fact]
    public void Validate_ExpandFieldNameAsScalar_IsReported()
    {
        // "_expand" telt niet als geldig scalair veld.
        var selection = new FieldSelection { ScalarFields = { "_expand" } };

        Assert.Equal(["_expand"], Validator().Validate(selection));
    }

    // ---- Geneste sub-entiteiten ----

    [Fact]
    public void Validate_ValidNestedEntity_WithValidChildScalar_ReturnsEmpty()
    {
        var selection = new FieldSelection { Entities = { ["zaaktype"] = new FieldSelection { ScalarFields = { "naam" } } } };

        Assert.Empty(Validator("zaaktype").Validate(selection));
    }

    [Fact]
    public void Validate_NestedEntity_NotExpandable_IsReported()
    {
        // Schema kent "status" wél, maar het is niet als expandbaar pad geregistreerd.
        var selection = new FieldSelection { Entities = { ["status"] = new FieldSelection() } };

        Assert.Equal(["status"], Validator("zaaktype").Validate(selection));
    }

    [Fact]
    public void Validate_NestedEntity_UnknownInSchema_IsReported()
    {
        // Pad is "expandbaar", maar het schema kent de sub-entiteit niet → ongeldig.
        var selection = new FieldSelection { Entities = { ["verzonnen"] = new FieldSelection() } };

        Assert.Equal(["verzonnen"], Validator("verzonnen").Validate(selection));
    }

    [Fact]
    public void Validate_InvalidChildScalar_IsReportedWithDottedPath()
    {
        var selection = new FieldSelection { Entities = { ["zaaktype"] = new FieldSelection { ScalarFields = { "fout" } } } };

        Assert.Equal(["zaaktype.fout"], Validator("zaaktype").Validate(selection));
    }

    [Fact]
    public void Validate_DeepNesting_ReportsFullDottedPath()
    {
        var selection = new FieldSelection
        {
            Entities = { ["zaaktype"] = new FieldSelection { Entities = { ["detail"] = new FieldSelection { ScalarFields = { "fout" } } } } },
        };

        var invalid = Validator("zaaktype", "zaaktype.detail").Validate(selection);

        Assert.Equal(["zaaktype.detail.fout"], invalid);
    }

    [Fact]
    public void Validate_MultipleInvalid_AreReturnedSortedOrdinal()
    {
        var selection = new FieldSelection { ScalarFields = { "zzz", "aaa" }, Entities = { ["status"] = new FieldSelection() } };

        var invalid = Validator("zaaktype").Validate(selection);

        // Ordinaal gesorteerd: "aaa" < "status" < "zzz".
        Assert.Equal(["aaa", "status", "zzz"], invalid);
    }

    // ---- Inline geneste objecten ----

    // poly is polymorf (expliciet); verlenging/kenmerken/diep/adres gaan via reflectie.
    private static FieldsValidator<HostDto> NestedValidator() =>
        new(
            new FieldsSchemaBuilder().NestedObject<HostDto, VariantOneDto>("poly").NestedObject<HostDto, VariantTwoDto>("poly").Build(),
            expandablePaths: []
        );

    [Fact]
    public void Validate_NestedObject_ValidInnerScalar_ReturnsEmpty()
    {
        var selection = new FieldSelection { NestedObjects = { ["verlenging"] = new FieldSelection { ScalarFields = { "a" } } } };

        Assert.Empty(NestedValidator().Validate(selection));
    }

    [Fact]
    public void Validate_NestedObject_UnknownInnerScalar_IsReportedWithDottedPath()
    {
        var selection = new FieldSelection { NestedObjects = { ["verlenging"] = new FieldSelection { ScalarFields = { "onbekend" } } } };

        Assert.Equal(["verlenging.onbekend"], NestedValidator().Validate(selection));
    }

    [Fact]
    public void Validate_NestedObject_DeepReflection_ReturnsEmpty()
    {
        // verlenging → InnerDto → diep → DeepDto, alles via reflectie.
        var selection = new FieldSelection
        {
            NestedObjects = { ["verlenging"] = new FieldSelection { NestedObjects = { ["diep"] = new FieldSelection { ScalarFields = { "x" } } } } },
        };

        Assert.Empty(NestedValidator().Validate(selection));
    }

    [Fact]
    public void Validate_NestedCollection_ValidInnerScalar_ReturnsEmpty()
    {
        var selection = new FieldSelection { NestedObjects = { ["kenmerken"] = new FieldSelection { ScalarFields = { "a" } } } };

        Assert.Empty(NestedValidator().Validate(selection));
    }

    [Fact]
    public void Validate_ScalarLeafWithDottedPath_IsReported()
    {
        // "uuid" is een string-scalar, geen genest object → afdalen is ongeldig.
        var selection = new FieldSelection { NestedObjects = { ["uuid"] = new FieldSelection { ScalarFields = { "x" } } } };

        Assert.Equal(["uuid"], NestedValidator().Validate(selection));
    }

    [Theory]
    [InlineData("een")] // VariantOne
    [InlineData("twee")] // VariantTwo
    public void Validate_PolymorphicNested_FieldFromAnyVariant_IsValid(string field)
    {
        var selection = new FieldSelection { NestedObjects = { ["poly"] = new FieldSelection { ScalarFields = { field } } } };

        Assert.Empty(NestedValidator().Validate(selection));
    }

    [Fact]
    public void Validate_PolymorphicNested_UnknownField_IsReported()
    {
        var selection = new FieldSelection { NestedObjects = { ["poly"] = new FieldSelection { ScalarFields = { "onzin" } } } };

        Assert.Equal(["poly.onzin"], NestedValidator().Validate(selection));
    }

    [Fact]
    public void Validate_PolymorphicNested_DeepFieldViaOneVariant_IsValid()
    {
        // poly.adres bestaat alleen op VariantOne; via de unie geldig, en adres → DeepDto via reflectie.
        var selection = new FieldSelection
        {
            NestedObjects = { ["poly"] = new FieldSelection { NestedObjects = { ["adres"] = new FieldSelection { ScalarFields = { "x" } } } } },
        };

        Assert.Empty(NestedValidator().Validate(selection));
    }
}
