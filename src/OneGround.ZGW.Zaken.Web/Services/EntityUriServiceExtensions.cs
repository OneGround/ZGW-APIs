using System;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Zaken.Web.Services;

public static class EntityUriServiceExtensions
{
    /// <summary>
    /// Builds the url of the zaak with the given id, in the same notation as the url of a zaak itself (<c>Zaak.Url</c> resolved by the service).
    /// </summary>
    public static string GetZaakUri(this IEntityUriService uriService, Guid zaakId) => uriService.GetUri("zaken", zaakId.ToString());
}
