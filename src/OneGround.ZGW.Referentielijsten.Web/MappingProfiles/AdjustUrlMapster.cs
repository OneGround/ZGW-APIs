using System;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Referentielijsten.Web.MappingProfiles;

/// <summary>
/// Mapster boundary for the AutoMapper <c>AdjustUrl</c> resolver. Resolves
/// <see cref="IHttpContextAccessor"/> from <see cref="MapContext"/>, so mapping must run
/// through ServiceMapper. Rewrite logic lives in <see cref="RequestUrlRewriter"/>.
/// </summary>
public static class AdjustUrlMapster
{
    public static string Adjust(string sourceUrl)
    {
        var mapContext =
            MapContext.Current
            ?? throw new InvalidOperationException("AdjustUrlMapster.Adjust must run through ServiceMapper: MapContext.Current is null.");

        var httpContextAccessor =
            mapContext.GetService<IHttpContextAccessor>()
            ?? throw new InvalidOperationException("IHttpContextAccessor is not registered on the Mapster service provider.");

        return RequestUrlRewriter.Rewrite(sourceUrl, httpContextAccessor.HttpContext?.Request);
    }
}
