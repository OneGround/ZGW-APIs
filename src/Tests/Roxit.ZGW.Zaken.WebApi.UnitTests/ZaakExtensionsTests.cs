using System.Collections.Generic;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.Extensions;
using Roxit.ZGW.Zaken.DataModel.ZaakRol;
using Xunit;

namespace Roxit.ZGW.Zaken.WebApi.UnitTests;

public class ZaakExtensionsTests
{
    [Fact]
    public void Zaak_DoesNotContain_ConversionKenmerk()
    {
        var zaak = new Zaak();

        Assert.False(zaak.HasConversionKenmerk());
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    public void Zaak_Contains_ConversionKenmerk_CaseInsensitive(string boolValue)
    {
        var zaak = new Zaak { Kenmerken = [new ZaakKenmerk { Bron = ZaakExtensions.ConversionBron, Kenmerk = boolValue }] };

        Assert.True(zaak.HasConversionKenmerk());
    }

    [Fact]
    public void ZaakEntity_DoesNotContain_ConversionKenmerk()
    {
        var zaakRol = new ZaakRol { Zaak = new Zaak() };

        Assert.False(zaakRol.HasConversionKenmerk());
    }

    [Fact]
    public void ZaakEntity_Contains_ConversionKenmerk()
    {
        var zaakRol = new ZaakRol { Zaak = new Zaak { Kenmerken = [new ZaakKenmerk { Bron = ZaakExtensions.ConversionBron, Kenmerk = "true" }] } };

        Assert.True(zaakRol.HasConversionKenmerk());
    }
}
