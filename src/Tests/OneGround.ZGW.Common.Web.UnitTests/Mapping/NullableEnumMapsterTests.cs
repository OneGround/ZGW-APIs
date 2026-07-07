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

    // A type carrying a Nullable<enum> property so the registrar discovers Colour? in this assembly.
    private sealed class Holder
    {
        public Colour? Colour { get; set; }
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
}
