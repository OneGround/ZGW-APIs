using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "communicatiekanaal" expand path from the communicatiekanaal url of the ZAAK.
/// </summary>
public class ZaakCommunicatiekanaalResolver : ZaakExternalJsonResolver
{
    public ZaakCommunicatiekanaalResolver(IExternalJsonClient externalJsonClient)
        : base(externalJsonClient) { }

    public override string Path => "communicatiekanaal";

    protected override string GetUrl(ZaakResponseDto zaak) => zaak.Communicatiekanaal;
}
