using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Web.Filters;

//
// Based on the ideas described in: https://referbruv.com/blog/how-to-build-a-simple-etag-in-aspnet-core/

// Prevents the action filter methods to be invoked twice
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ETagFilter : ActionFilterAttribute
{
    private readonly string[] _supportedVerbs = [HttpMethod.Get.Method, HttpMethod.Head.Method];

    public override async Task OnActionExecutionAsync(ActionExecutingContext executingContext, ActionExecutionDelegate next)
    {
        var request = executingContext.HttpContext.Request;

        var executedContext = await next();

        var response = executedContext.HttpContext.Response;

        if (_supportedVerbs.Contains(request.Method) && response.StatusCode == (int)HttpStatusCode.OK)
        {
            ValidateETagForResponseCaching(executedContext);
        }
    }

    private static void ValidateETagForResponseCaching(ActionExecutedContext executedContext)
    {
        if (executedContext.Result == null)
        {
            return;
        }

        var request = executedContext.HttpContext.Request;
        var response = executedContext.HttpContext.Response;

        var result = executedContext.Result;

        // Generates ETag from the entire response Content
        var etag = GenerateEtagFromResponseBodyWithHash(result);

        if (request.Headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.IfNoneMatch, out var ifNoneMatchValue))
        {
            // Fetch etag from the incoming request header
            var incomingEtag = ifNoneMatchValue.ToString();

            // If both the etags are equal raise a 304 Not Modified Response
            if (incomingEtag.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Any(e => e == $"\"{etag}\""))
            {
                executedContext.Result = new StatusCodeResult((int)HttpStatusCode.NotModified);
            }
        }

        // Add ETag response header
        response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ETag] = new[] { $"\"{etag}\"" };
    }

    private static string GenerateEtagFromResponseBodyWithHash(IActionResult tmpSource)
    {
        return ETagService.ComputeWithHashFunction(tmpSource);
    }
}

internal static class ETagService
{
    public static string ComputeWithHashFunction(object value)
    {
        // Note: Set ReferenceLoopHandling to Ignore: Serializing Geometrie throws an exception when this is not set:
        //         Newtonsoft.Json.JsonSerializationException: Self referencing loop detected for property 'CoordinateValue' with type 'NetTopologySuite.Geometries.Coordinate'. Path 'Value.Zaakgeometrie.Coordinates[0]'
        var serialized = JsonConvert.SerializeObject(value, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));

        return Convert.ToBase64String(hash);
    }
}
