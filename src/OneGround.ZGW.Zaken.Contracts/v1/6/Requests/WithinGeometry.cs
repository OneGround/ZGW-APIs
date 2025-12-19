using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._6.Requests;

public class WithinGeometry
{
    [JsonProperty("within")]
    public Geometry Within { get; set; }
}
