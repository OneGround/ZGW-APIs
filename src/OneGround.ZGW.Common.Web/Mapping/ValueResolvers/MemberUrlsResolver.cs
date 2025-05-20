using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Mapping.ValueResolvers;

public class MemberUrlsResolver : IMemberValueResolver<object, object, IEnumerable<IUrlEntity>, IEnumerable<string>>
{
    private readonly IEntityUriService _uriService;

    public MemberUrlsResolver(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public IEnumerable<string> Resolve(
        object source,
        object destination,
        IEnumerable<IUrlEntity> sourceMember,
        IEnumerable<string> destMember,
        ResolutionContext context
    )
    {
        return sourceMember?.Select(s => _uriService.GetUri(s));
    }
}
