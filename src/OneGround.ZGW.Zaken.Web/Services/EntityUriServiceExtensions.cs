using System;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.Web.Services;

/// <summary>
/// Builds the url of a zaak from its id. The two methods only differ in the address of the url, so pick the one that fits the use of the url.
/// </summary>
public static class EntityUriServiceExtensions
{
    /// <summary>
    /// The url of the zaak on the address of the current request, in the same notation as the url of a zaak itself (<c>Zaak.Url</c> resolved by
    /// the service). Use this for urls that are returned in a response.
    /// </summary>
    public static string GetRequestZaakUri(this IEntityUriService uriService, Guid zaakId) => uriService.GetUri(new ZaakReference(zaakId));

    /// <summary>
    /// The url of the zaak on the configured ZRC address (service discovery). Use this for urls that are looked up through the zaken service agent,
    /// which only accepts urls of that address, whatever address the current request came in on.
    /// </summary>
    public static string GetZrcZaakUri(this IEntityUriService uriService, Guid zaakId) =>
        uriService.GetUri(ServiceRoleName.ZRC, new ZaakReference(zaakId));

    private sealed record ZaakReference(Guid Id) : IUrlEntity
    {
        public string Url => $"/zaken/{Id}";
    }
}
