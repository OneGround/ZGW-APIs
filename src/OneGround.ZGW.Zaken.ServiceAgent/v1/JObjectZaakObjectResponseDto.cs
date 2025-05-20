using Newtonsoft.Json.Linq;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

namespace OneGround.ZGW.Zaken.ServiceAgent.v1;

public class JObjectZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<JObject>
{
    public JObject ObjectIdentificatie { get; set; }

    public OverigeZaakObjectDto OverigeZaakObject => ObjectIdentificatie.ToObject<OverigeZaakObjectDto>();
}
