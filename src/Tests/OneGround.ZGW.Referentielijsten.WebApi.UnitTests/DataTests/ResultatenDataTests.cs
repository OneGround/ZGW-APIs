using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OneGround.ZGW.Referentielijsten.Contracts.v1;
using OneGround.ZGW.Referentielijsten.Web.Models;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.DataTests;

public class ResultatenDataTests
{
    [Fact]
    public void EveryProcestermijnInTheData_IsAKnownProcestermijn()
    {
        using var stream = typeof(Resultaat).Assembly.GetManifestResourceStream("OneGround.ZGW.Referentielijsten.Web.Data.Resultaten_data.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var resultaten = JsonConvert.DeserializeObject<IList<Resultaat>>(reader.ReadToEnd());

        // Empty means "no procestermijn" (maps to null); every other value must exist in the Procestermijn enum.
        var unknown = resultaten
            .Select(r => r.Procestermijn)
            .Where(v => !string.IsNullOrEmpty(v) && !Enum.IsDefined(typeof(Procestermijn), v))
            .Distinct()
            .ToList();

        Assert.True(
            unknown.Count == 0,
            $"Procestermijn value(s) in Resultaten_data.json missing from the Procestermijn enum: {string.Join(", ", unknown)}"
        );
    }
}
