using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OneGround.ZGW.Referentielijsten.Contracts.v1;
using OneGround.ZGW.Referentielijsten.Web.Models;

namespace OneGround.ZGW.Referentielijsten.Web.Services;

public class ReferentielijstenDataService
{
    public readonly IReadOnlyDictionary<Guid, CommunicatieKanaal> CommunicatieKanalen;
    public readonly IReadOnlyDictionary<Guid, ResultaatTypeOmschrijving> ResultaatTypeOmschrijvingen;
    public readonly IReadOnlyDictionary<Guid, Resultaat> Resultaten;
    public readonly IReadOnlyDictionary<Guid, ProcesType> ProcesTypen;

    public ReferentielijstenDataService()
    {
        CommunicatieKanalen = SeedCommunicatieKanalen();
        ResultaatTypeOmschrijvingen = SeedResultaatTypeOmschrijvingen();
        Resultaten = SeedResultaten();
        ProcesTypen = SeedProcesTypen();
    }

    private static Dictionary<Guid, ProcesType> SeedProcesTypen()
    {
        var procesTypen = ReadAndDeserializeJson<IList<ProcesType>>("OneGround.ZGW.Referentielijsten.Web.Data.Proces_typen_data.json");
        var procesTypes = new Dictionary<Guid, ProcesType>();
        foreach (var item in procesTypen)
        {
            var guid = GetGuidFromUrl(item.Url);
            procesTypes.Add(guid, item);
        }

        return procesTypes;
    }

    private static Dictionary<Guid, ResultaatTypeOmschrijving> SeedResultaatTypeOmschrijvingen()
    {
        var resultaatTypeOmschrijvingen = ReadAndDeserializeJson<IList<ResultaatTypeOmschrijving>>(
            "OneGround.ZGW.Referentielijsten.Web.Data.Resultaat_type_omschrijvingen_data.json"
        );
        var resultaatTypeOmschrijvings = new Dictionary<Guid, ResultaatTypeOmschrijving>();

        foreach (var item in resultaatTypeOmschrijvingen)
        {
            resultaatTypeOmschrijvings.Add(item.Id, item);
        }

        return resultaatTypeOmschrijvings;
    }

    private static Dictionary<Guid, Resultaat> SeedResultaten()
    {
        var resultaten = ReadAndDeserializeJson<IList<Resultaat>>("OneGround.ZGW.Referentielijsten.Web.Data.Resultaten_data.json");
        ValidateEnums(resultaten);
        var resultaats = new Dictionary<Guid, Resultaat>();

        foreach (var item in resultaten)
        {
            var guid = GetGuidFromUrl(item.Url);
            resultaats.Add(guid, item);
        }

        return resultaats;
    }

    /// <summary>
    /// Fails with a descriptive message when a resultaat has a value for an enum field that is not a valid member of that enum, so a data error stops the
    /// start with the offending records instead of failing on a request.
    /// </summary>
    public static void ValidateEnums(IEnumerable<Resultaat> resultaten)
    {
        var invalid = new List<string>();
        foreach (var r in resultaten)
        {
            if (!ReferentielijstEnum.TryParse<Procestermijn>(r.Procestermijn, out _))
            {
                invalid.Add($"procestermijn '{r.Procestermijn}' ({r.Url})");
            }

            if (!ReferentielijstEnum.TryParse<Waardering>(r.Waardering, out _))
            {
                invalid.Add($"waardering '{r.Waardering}' ({r.Url})");
            }
        }

        if (invalid.Count > 0)
        {
            throw new InvalidOperationException(
                $"Resultaten_data.json contains {invalid.Count} invalid value(s): {string.Join(", ", invalid)}. "
                    + $"Allowed procestermijn values: {string.Join(", ", Enum.GetNames<Procestermijn>())}; allowed waardering values: {string.Join(", ", Enum.GetNames<Waardering>())}."
            );
        }
    }

    private static Dictionary<Guid, CommunicatieKanaal> SeedCommunicatieKanalen()
    {
        var kanalen = ReadAndDeserializeJson<IList<CommunicatieKanaal>>("OneGround.ZGW.Referentielijsten.Web.Data.Communicatie_kanalen_data.json");
        var communicatieKanaals = new Dictionary<Guid, CommunicatieKanaal>();
        foreach (var item in kanalen)
        {
            var guid = GetGuidFromUrl(item.Url);
            communicatieKanaals.Add(guid, item);
        }

        return communicatieKanaals;
    }

    private static Guid GetGuidFromUrl(string url)
    {
        var guid = new Guid(url.Split("/").Last());
        return guid;
    }

    private static T ReadAndDeserializeJson<T>(string resourceName)
    {
        var data = LoadEmbeddedResourceAsString(typeof(ReferentielijstenDataService).Assembly, resourceName);
        return JsonConvert.DeserializeObject<T>(data);
    }

    private static string LoadEmbeddedResourceAsString(Assembly assembly, string resourceName)
    {
        if (!assembly.GetManifestResourceNames().Contains(resourceName))
        {
            var unableLoadResourceExceptionMessage = $"UnableToLoadEmbeddedResource: {resourceName}";
            throw new ArgumentException(unableLoadResourceExceptionMessage);
        }

        using var streamReader = new StreamReader(assembly.GetManifestResourceStream(resourceName)!);
        return streamReader.ReadToEnd();
    }
}
