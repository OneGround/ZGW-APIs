using Newtonsoft.Json;

namespace OneGround.ZGW.Gesprekken.Contracts.v1.Responses;

public class BestandResponseDto
{
    [JsonProperty("fileId", Order = 1)]
    public string FileId { get; set; }

    [JsonProperty("url", Order = 2)]
    public string Url { get; set; }

    [JsonProperty("bestandsnaam", Order = 3)]
    public string Bestandsnaam { get; set; }

    [JsonProperty("bestandsomvang", Order = 4)]
    public long Bestandsomvang { get; set; }

    [JsonProperty("contentType", Order = 5)]
    public string ContentType { get; set; }

    [JsonProperty("titel", Order = 6)]
    public string Titel { get; set; }

    [JsonProperty("status", Order = 7)]
    public string Status { get; set; }

    [JsonProperty("aangemaaktOp", Order = 8)]
    public string AangemaaktOp { get; set; }

    [JsonProperty("geuploadOp", Order = 9)]
    public string GeuploadOp { get; set; }

    [JsonProperty("downloadUrl", Order = 10, NullValueHandling = NullValueHandling.Ignore)]
    public string DownloadUrl { get; set; }
}

public class UploadSessieResponseDto
{
    [JsonProperty("fileId", Order = 1)]
    public string FileId { get; set; }

    [JsonProperty("uploadUrl", Order = 2)]
    public string UploadUrl { get; set; }

    [JsonProperty("verlooptOp", Order = 3)]
    public string VerlooptOp { get; set; }

    [JsonProperty("bestand", Order = 4)]
    public BestandResponseDto Bestand { get; set; }
}
