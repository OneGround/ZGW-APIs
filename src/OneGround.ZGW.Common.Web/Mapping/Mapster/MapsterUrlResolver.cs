using Mapster;
using MapsterMapper;
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
}
