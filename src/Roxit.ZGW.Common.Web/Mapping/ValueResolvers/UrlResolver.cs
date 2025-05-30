using AutoMapper;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Common.Web.Mapping.ValueResolvers;

public class UrlResolver : IValueResolver<IUrlEntity, object, string>
{
    private readonly IEntityUriService _uriService;

    public UrlResolver(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public string Resolve(IUrlEntity source, object destination, string destMember, ResolutionContext context)
    {
        if (source != null)
        {
            return _uriService.GetUri(source);
        }

        return null;
    }
}
