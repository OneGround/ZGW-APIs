using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// Request bodies shared by the integration tests.
/// </summary>
internal static class FlowBody
{
    public const string Partij = "https://klantinteracties.integrationtest.invalid/api/v1/partijen/cccccccc-cccc-cccc-cccc-cccccccccccc";
    private const string Actor = "https://klantinteracties.integrationtest.invalid/api/v1/actoren/medewerker";

    public static JObject Deelname(string type) =>
        type == "partij"
            ? new JObject
            {
                ["type"] = "partij",
                ["partij"] = Partij,
                ["naam"] = "Piet",
            }
            : new JObject
            {
                ["type"] = "actor",
                ["actor"] = Actor,
                ["naam"] = "Gemeente",
            };

    public static JObject NewGesprek(string zaak) =>
        new()
        {
            ["gespreksonderwerp"] = "Aanvraag bijzondere bijstand",
            ["aanleidingZaak"] = zaak,
            ["ontvanger"] = new JArray(Deelname("partij")),
            ["eersteBijdrage"] = new JObject
            {
                ["tekstGespreksBijdrage"] = "Wij hebben nog een vraag over uw aanvraag.",
                ["verzender"] = new JObject { ["nieuweDeelname"] = Deelname("actor") },
            },
        };
}
