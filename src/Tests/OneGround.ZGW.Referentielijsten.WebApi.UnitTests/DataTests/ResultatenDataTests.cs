using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OneGround.ZGW.Referentielijsten.Web.Models;
using OneGround.ZGW.Referentielijsten.Web.Services;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.DataTests;

public class ResultatenDataTests
{
    [Fact]
    public void TheResultatenData_HasOnlyValidEnumValues()
    {
        using var stream = typeof(Resultaat).Assembly.GetManifestResourceStream("OneGround.ZGW.Referentielijsten.Web.Data.Resultaten_data.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var resultaten = JsonConvert.DeserializeObject<IList<Resultaat>>(reader.ReadToEnd());

        ReferentielijstenDataService.ValidateEnums(resultaten);
    }

    [Fact]
    public void ValidateEnums_WithUnknownProcestermijn_ThrowsDescriptiveError()
    {
        var resultaten = new[]
        {
            new Resultaat { Url = "https://example.test/resultaten/1", Procestermijn = "nihil" },
            new Resultaat { Url = "https://example.test/resultaten/2", Procestermijn = "" },
            new Resultaat { Url = "https://example.test/resultaten/3", Procestermijn = "foo" },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => ReferentielijstenDataService.ValidateEnums(resultaten));

        Assert.Contains("procestermijn 'foo' (https://example.test/resultaten/3)", ex.Message);
        Assert.DoesNotContain("resultaten/1", ex.Message);
        Assert.Contains("na_einde_procesobject", ex.Message);
    }

    [Fact]
    public void ValidateEnums_WithKnownEmptyOrNullProcestermijn_DoesNotThrow()
    {
        var resultaten = new[]
        {
            new Resultaat { Url = "https://example.test/resultaten/1", Procestermijn = "nihil" },
            new Resultaat { Url = "https://example.test/resultaten/2", Procestermijn = "" },
            new Resultaat { Url = "https://example.test/resultaten/3", Procestermijn = null },
        };

        ReferentielijstenDataService.ValidateEnums(resultaten);
    }

    [Fact]
    public void ValidateEnums_WithUnknownWaardering_ThrowsDescriptiveError()
    {
        var resultaten = new[]
        {
            new Resultaat
            {
                Url = "https://example.test/resultaten/1",
                Procestermijn = "nihil",
                Waardering = "vernietigen",
            },
            new Resultaat
            {
                Url = "https://example.test/resultaten/2",
                Procestermijn = "",
                Waardering = "bewaren",
            },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => ReferentielijstenDataService.ValidateEnums(resultaten));

        Assert.Contains("waardering 'bewaren' (https://example.test/resultaten/2)", ex.Message);
        Assert.DoesNotContain("resultaten/1", ex.Message);
    }
}
