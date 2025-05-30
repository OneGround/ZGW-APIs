using AutoMapper;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Common.Web.Mapping.ValueResolvers;

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
