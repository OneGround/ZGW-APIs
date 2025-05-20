using AutoMapper;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Mapping.ValueResolvers;

public class MemberUrlResolver : IMemberValueResolver<object, object, IUrlEntity, string>
{
    private readonly IEntityUriService _uriService;

    public MemberUrlResolver(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public string Resolve(object source, object destination, IUrlEntity sourceMember, string destMember, ResolutionContext context)
    {
        if (sourceMember != null)
        {
            return _uriService.GetUri(sourceMember);
        }

        return null;
    }
}
