using System;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Referentielijsten.Web.MappingProfiles;

public class AdjustUrl : IMemberValueResolver<object, object, string, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdjustUrl(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Resolve(object source, object destination, string sourceMember, string destMember, ResolutionContext context)
    {
        var uriBuilder = new UriBuilder(sourceMember);
        if (_httpContextAccessor.HttpContext.Request.Host.HasValue)
        {
            uriBuilder.Host = _httpContextAccessor.HttpContext.Request.Host.Host;
        }
        if (_httpContextAccessor.HttpContext.Request.Host.Port.HasValue)
        {
            uriBuilder.Port = _httpContextAccessor.HttpContext.Request.Host.Port.Value;
        }
        if (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext.Request.Scheme))
        {
            uriBuilder.Scheme = _httpContextAccessor.HttpContext.Request.Scheme;
        }

        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.ToString();
    }
}
