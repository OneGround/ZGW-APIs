using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Common.Web.Services.UriServices;

public abstract class BaseUriService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected BaseUriService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected string BaseUri
    {
        get
        {
            var request = _httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host.ToUriComponent()}/";
        }
    }

    protected string Path => _httpContextAccessor.HttpContext.Request.Path.Value;
}
