using System.Collections.Generic;
using System.Linq;
using Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Mapping.Mapster;

/// <summary>
/// Mapster replacement for UrlResolver / MemberUrlResolver: resolves an <see cref="IUrlEntity"/>
/// to its URL using the DI-registered <see cref="IEntityUriService"/>. Requires mapping to run
/// through ServiceMapper so the service provider is available on MapContext.
/// </summary>
public static class MapsterUrlResolver
{
    public static string ResolveUrl(IUrlEntity entity)
    {
        if (entity == null)
        {
            return null;
        }

        var uriService = MapContext.Current.GetService<IEntityUriService>();
        return uriService.GetUri(entity);
    }

    /// <summary>
    /// Mapster replacement for MemberUrlsResolver: resolves a collection of <see cref="IUrlEntity"/>
    /// to their URLs using the DI-registered <see cref="IEntityUriService"/>. Requires mapping to run
    /// through ServiceMapper so the service provider is available on MapContext. The service is
    /// resolved once (fetched outside the projection lambda) regardless of laziness. Unlike the
    /// original AutoMapper <c>MemberUrlsResolver</c>, which returns a genuinely lazy
    /// <c>sourceMember?.Select(...)</c>, this materializes eagerly (.ToList()) — a deliberate, stricter
    /// guarantee: the mapping runs inside the request scope, and the enumerable must not be iterated
    /// after that scope is gone.
    /// </summary>
    public static IEnumerable<string> ResolveUrls(IEnumerable<IUrlEntity> entities)
    {
        if (entities == null)
        {
            return null;
        }

        var uriService = MapContext.Current.GetService<IEntityUriService>();
        return entities.Select(e => uriService.GetUri(e)).ToList();
    }
}
