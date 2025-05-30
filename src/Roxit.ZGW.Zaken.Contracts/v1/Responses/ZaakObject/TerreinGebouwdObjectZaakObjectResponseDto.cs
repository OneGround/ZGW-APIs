using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

public class TerreinGebouwdObjectZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<TerreinGebouwdObjectZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public TerreinGebouwdObjectZaakObjectDto ObjectIdentificatie { get; set; }
}
