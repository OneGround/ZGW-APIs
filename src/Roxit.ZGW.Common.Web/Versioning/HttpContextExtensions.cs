using System;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;

namespace Roxit.ZGW.Common.Web.Versioning;

public static class HttpContextExtensions
{
    public static ZgwApiVersion GetRequestedZgwApiVersion(this HttpContext context)
    {
        var result = context.ApiVersioningFeature().RequestedApiVersion as ZgwApiVersion;

        return result;
    }

    private static IApiVersioningFeature ApiVersioningFeature(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var apiVersioningFeature = context.Features.Get<IApiVersioningFeature>();

        if (apiVersioningFeature != null)
        {
            return apiVersioningFeature;
        }

        apiVersioningFeature = new ApiVersioningFeature(context);
        context.Features.Set(apiVersioningFeature);

        return apiVersioningFeature;
    }
}
