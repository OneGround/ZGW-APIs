using System;
using System.Reflection;
using Mapster;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Mapping;

public class NullableEnumMapsterTests
{
    private enum Colour
    {
        Red,
        Green,
    }

    private enum Size
    {
        Small,
        Large,
    }

    // A type carrying a Nullable<enum> property so the registrar discovers Colour? in this assembly.
    private sealed class Holder
    {
        public Colour? Colour { get; set; }
        public Size? Size { get; set; }
    }

    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        config.RegisterNullableEnumRules(new[] { typeof(NullableEnumMapsterTests).Assembly });
        config.Compile();
        return config;
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_or_null_string_maps_to_null(string source)
    {
        var config = BuildConfig();
        var result = source.Adapt<Colour?>(config);
        Assert.Null(result);
    }

    [Fact]
    public void Valid_enum_name_maps_to_value()
    {
        var config = BuildConfig();
        var result = "Green".Adapt<Colour?>(config);
        Assert.Equal(Colour.Green, result);
    }

    [Fact]
    public void Unknown_enum_name_maps_to_null()
    {
        var config = BuildConfig();
        var result = "Purple".Adapt<Colour?>(config);
        Assert.Null(result);
    }

    [Fact]
    public void Enum_type_from_unregistered_assembly_is_not_covered_by_rule()
    {
        // Registering against an assembly that does not declare Colour means no rule is
        // registered for Colour?, so Mapster falls back to its default string->enum behavior,
        // which throws on an unrecognized value instead of returning null the way our rule does.
        var config = new TypeAdapterConfig();
        config.RegisterNullableEnumRules(Array.Empty<Assembly>());
        config.Compile();

        Assert.ThrowsAny<Exception>(() => "Purple".Adapt<Colour?>(config));
    }

    [Fact]
    public void Two_distinct_enum_types_each_get_independent_correct_rules()
    {
        var config = BuildConfig();

        var colourResult = "Green".Adapt<Colour?>(config);
        var unknownColourResult = "Purple".Adapt<Colour?>(config);
        var sizeResult = "Large".Adapt<Size?>(config);
        var unknownSizeResult = "Medium".Adapt<Size?>(config);

        Assert.Equal(Colour.Green, colourResult);
        Assert.Null(unknownColourResult);
        Assert.Equal(Size.Large, sizeResult);
        Assert.Null(unknownSizeResult);
    }
}
