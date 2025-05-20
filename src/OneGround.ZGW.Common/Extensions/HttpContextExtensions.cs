using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Authentication;

namespace OneGround.ZGW.Common.Extensions;

public static class HttpContextExtensions
{
    public static string GetUserRepresentation(this HttpContext httpContext)
    {
        var value = httpContext.User.FindFirst(c => c.Type == CustomClaimTypes.UserRepresentation)?.Value;
        return value ?? string.Empty;
    }

    public static string GetUserId(this HttpContext httpContext)
    {
        var userId = httpContext.User?.FindFirst(c => c.Type == CustomClaimTypes.UserId)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
            return userId;

        if (httpContext.Request.Headers.TryGetValue("X-NLX-Request-User-Id", out var value))
            return value;

        return userId ?? string.Empty;
    }

    public static string GetClientId(this HttpContext httpContext)
    {
        var clientId = httpContext.User?.FindFirst(c => c.Type == CustomClaimTypes.ClientId)?.Value;
        if (!string.IsNullOrWhiteSpace(clientId))
            return clientId;

        if (httpContext.Request.Headers.TryGetValue("X-NLX-Request-Application-Id", out var value))
            return value;

        return clientId ?? string.Empty;
    }

    public static string GetRsin(this HttpContext httpContext)
    {
        return httpContext.User.FindFirst(c => c.Type == CustomClaimTypes.Rsin)?.Value;
    }

    // "Accept-Crs":
    //  Het gewenste 'Coordinate Reference System' (CRS) van de geometrie in het antwoord (response body).
    //  Volgens de GeoJSON spec is WGS84 de default (EPSG:4326 is hetzelfde als WGS84).
    public static string GetAcceptCrsHeader(this HttpContext httpContext)
    {
        var acceptCrsHeader = httpContext.GetHeader("Accept-Crs");
        return acceptCrsHeader;
    }

    // "Content-Crs":
    //  Het 'Coordinate Reference System' (CRS) van de geometrie in de vraag (request body).
    //  Volgens de GeoJSON spec is WGS84 de default (EPSG:4326 is hetzelfde als WGS84).
    public static string GetContentCrsHeader(this HttpContext httpContext)
    {
        var contentCrsHeader = httpContext.GetHeader("Content-Crs");
        return contentCrsHeader;
    }

    public static string GetApplicationLabel(this HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue("authorizedApplicationLabel", out var value) ? value.ToString() : string.Empty;
    }

    public static string GetRequestId(this HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-NLX-Request-Id", out var value))
            return value;

        return httpContext.TraceIdentifier;
    }

    public static string GetHeader(this HttpContext httpContext, string headerName)
    {
        if (httpContext.Request.Headers.TryGetValue(headerName, out var value))
            return value;

        return string.Empty;
    }
}
