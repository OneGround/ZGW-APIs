using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

/// <summary>
/// Redactie (patterns/event-stream): the resource whose content was removed, and the events whose content is gone
/// from the history. Also the data of the gespreksbijdrage.geredigeerd event.
/// </summary>
public class RedactieResponseDto
{
    [JsonProperty("url", Order = 1)]
    public string Url { get; set; }

    [JsonProperty("geredigeerdeSequences", Order = 2)]
    public List<string> GeredigeerdeSequences { get; set; } = [];
}
