using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

public class TerreinGebouwdObjectZaakObjectRequestDto : ZaakObjectRequestDto, IRelatieZaakObjectDto<TerreinGebouwdObjectZaakObjectDto>
{
    [JsonProperty("objectIdentificatie", Order = 1000)]
    public TerreinGebouwdObjectZaakObjectDto ObjectIdentificatie { get; set; }
}
