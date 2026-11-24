using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OneGround.ZGW.Referentielijsten.Contracts.v1;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.DataTests;

public class ReferentielijstEnumTests
{
    [Theory]
    [InlineData(null, Procestermijn.leeg)]
    [InlineData("", Procestermijn.leeg)]
    [InlineData("nihil", Procestermijn.nihil)]
    [InlineData("na_einde_procesobject", Procestermijn.na_einde_procesobject)]
    public void Parse_KnownValue_ReturnsMember(string value, Procestermijn expected)
    {
        Assert.Equal(expected, ReferentielijstEnum.Parse<Procestermijn>(value));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("1")]
    [InlineData("NIHIL")]
    [InlineData("leeg")]
    public void TryParse_UnknownValue_ReturnsFalse(string value)
    {
        Assert.False(ReferentielijstEnum.TryParse<Procestermijn>(value, out _));
    }

    [Fact]
    public void Parse_UnknownValue_ThrowsMessageWithAllowedValues()
    {
        var ex = Assert.Throws<FormatException>(() => ReferentielijstEnum.Parse<Procestermijn>("foo"));

        Assert.Contains("'foo'", ex.Message);
        Assert.Contains("nihil", ex.Message);
        Assert.Contains("(empty)", ex.Message);
    }

    [Fact]
    public void Procestermijn_HasTheStringEnumConverterOnTheType_SoSwaggerShowsStrings()
    {
        var converter = typeof(Procestermijn).GetCustomAttribute<JsonConverterAttribute>();

        Assert.NotNull(converter);
        Assert.Equal(typeof(StringEnumConverter), converter.ConverterType);
    }

    [Theory]
    [InlineData(null, Waardering.leeg)]
    [InlineData("", Waardering.leeg)]
    [InlineData("blijvend_bewaren", Waardering.blijvend_bewaren)]
    [InlineData("vernietigen", Waardering.vernietigen)]
    public void Parse_KnownWaardering_ReturnsMember(string value, Waardering expected)
    {
        Assert.Equal(expected, ReferentielijstEnum.Parse<Waardering>(value));
    }

    [Fact]
    public void Waardering_HasTheStringEnumConverterOnTheType_SoSwaggerShowsStrings()
    {
        var converter = typeof(Waardering).GetCustomAttribute<JsonConverterAttribute>();

        Assert.NotNull(converter);
        Assert.Equal(typeof(StringEnumConverter), converter.ConverterType);
    }
}
