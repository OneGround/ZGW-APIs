using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

/// <summary>
/// A page of a list with cursor pagination (patterns/pagination, CursorPaginatedBase): follow next until it is null.
/// There is no previous.
/// </summary>
public class CursorPagedResponseDto<T>
{
    [JsonProperty("next", Order = 1)]
    public string Next { get; set; }

    [JsonProperty("count", Order = 2)]
    public int Count { get; set; }

    [JsonProperty("results", Order = 3)]
    public List<T> Results { get; set; } = [];
}
