using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace OneGround.ZGW.Common.Web.Expands;

public static class DtoExpander
{
    // Name properties the way MVC would: expand hands the client this JObject verbatim, so the
    // response pipeline never gets to apply its own naming. The converters stay attached.
    private static readonly IContractResolver CamelCaseResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

    public static object Merge(object main, object expand)
    {
        if (main == null)
        {
            throw new InvalidOperationException("Merging a null main object with the expanded object is not possible.");
        }
        if (expand == null)
        {
            throw new InvalidOperationException("Merging a main object with a null expanded object is not possible.");
        }

        var serializer = new ZGWJsonSerializer { ContractResolver = CamelCaseResolver };

        var jMain = JObject.FromObject(main, serializer);

        var settings = new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Union, // Merge arrays without duplicates
        };

        try
        {
            jMain.Merge(JObject.FromObject(expand, serializer), settings);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error while merging the main object with the expanded object.", ex);
        }
        return jMain;
    }
}
