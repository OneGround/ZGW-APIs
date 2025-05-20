using System;
using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services.UriServices;

public interface IEntityUriService
{
    string GetUri(IUrlEntity entity);
    string GetUri(string serviceRoleName, IUrlEntity entity);
    Guid GetId(string uri);
    string GetUri(params string[] segments);
}

public class UriService : BaseUriService, IEntityUriService
{
    private readonly IServiceDiscovery _serviceEndpoints;

    public UriService(IHttpContextAccessor httpContextAccessor, IServiceDiscovery serviceEndpoints)
        : base(httpContextAccessor)
    {
        _serviceEndpoints = serviceEndpoints;
    }

    public UriService(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    private static string Version => "v1";

    private static string BasePath => $"api/{Version}";

    [Obsolete("Use OneGround.ZGW.Common.Web.Helpers.GetResourceId(string uri) instead. This method will be removed in the future.")]
    public Guid GetId(string uri)
    {
        return UriHelper.GetResourceId(uri);
    }

    public string GetUri(IUrlEntity entity)
    {
        var builder = new UriBuilder(BaseUri) { Path = $"{BasePath}{entity.Url}" };
        return builder.Uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped);
    }

    public string GetUri(string serviceRoleName, IUrlEntity entity)
    {
        var servicePath = _serviceEndpoints.GetApi(serviceRoleName);
        var builder = new UriBuilder(servicePath) { Path = $"{BasePath}{entity.Url}" };
        return builder.Uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped);
    }

    public string GetUri(params string[] segments)
    {
        var relativePath = '/' + string.Join('/', segments).Trim('/');

        var builder = new UriBuilder(BaseUri) { Path = $"{BasePath}{relativePath}" };
        return builder.Uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped);
    }
}
