using AutoMapper;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Mapping.ValueResolvers;

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
