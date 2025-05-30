using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roxit.ZGW.Common.Contracts.v1;

public class ErrorResponse
{
    public ErrorResponse()
    {
        Instance = $"urn:uuid:{Guid.NewGuid()}";
        InvalidParams = [];
    }

    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }

    [JsonProperty(PropertyName = "code")]
    public string Code { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }

    [JsonProperty(PropertyName = "status")]
    public int Status { get; set; }

    [JsonProperty(PropertyName = "detail")]
    public string Detail { get; set; }

    [JsonProperty(PropertyName = "instance")]
    public string Instance { get; }

    [JsonProperty(PropertyName = "invalidParams")]
    public List<ValidationError> InvalidParams { get; set; }
}
