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

    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        config.RegisterNullableEnumRule();
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
    public void Two_distinct_enum_types_are_both_handled_by_the_same_global_rule()
    {
        // Neither Colour nor Size was ever scanned or passed anywhere — the single global rule
        // registered by BuildConfig() covers both automatically, which is the whole point of
        // replacing assembly scanning with a type-shape-based `When` rule.
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
