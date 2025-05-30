using Newtonsoft.Json.Linq;
using Roxit.ZGW.Zaken.Contracts.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;

namespace Roxit.ZGW.Zaken.ServiceAgent.v1;

public class JObjectZaakObjectResponseDto : ZaakObjectResponseDto, IRelatieZaakObjectDto<JObject>
{
    public JObject ObjectIdentificatie { get; set; }

    public OverigeZaakObjectDto OverigeZaakObject => ObjectIdentificatie.ToObject<OverigeZaakObjectDto>();
}
