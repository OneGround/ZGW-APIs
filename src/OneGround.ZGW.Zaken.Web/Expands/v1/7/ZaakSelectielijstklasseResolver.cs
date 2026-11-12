using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "selectielijstklasse" expand path from the selectielijstklasse url of the ZAAK.
/// </summary>
public class ZaakSelectielijstklasseResolver : ExternalJsonResolver<ZaakResponseDto>
{
    public ZaakSelectielijstklasseResolver(IExternalJsonClient externalJsonClient)
        : base(externalJsonClient) { }

    public override string Path => "selectielijstklasse";

    protected override string GetUrl(ZaakResponseDto zaak) => zaak.Selectielijstklasse;
}
