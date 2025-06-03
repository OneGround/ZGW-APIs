using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Roxit.ZGW.Common.Web.Filters;

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
        const int BufferSize = 16_384;

        var tempFilePath = Path.GetTempFileName();

        using (var rwstream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: BufferSize))
        {
            using var writer = new StreamWriter(rwstream);

            using var jsonWriter = new JsonTextWriter(writer);

            // Note: Set ReferenceLoopHandling to Ignore: Serializing Geometrie throws an exception when this is not set:
            //         Newtonsoft.Json.JsonSerializationException: Self referencing loop detected for property 'CoordinateValue' with type
            var serializer = new JsonSerializer() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            serializer.Serialize(jsonWriter, value);
        }

        using (
            var rstream = new FileStream(
                tempFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                bufferSize: BufferSize,
                FileOptions.DeleteOnClose
            )
        )
        {
            using var sha256 = SHA256.Create();

            byte[] hash = sha256.ComputeHash(rstream);

            return Convert.ToBase64String(hash);
        }
    }
}
