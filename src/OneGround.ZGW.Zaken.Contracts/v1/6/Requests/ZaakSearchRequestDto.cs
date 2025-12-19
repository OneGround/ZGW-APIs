using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Zaken.Contracts.v1._6.Requests;

public class ZaakSearchRequestDto
{
    [FromBody]
    public JObject Body { get; set; }
}
