using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

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

        if (executedContext.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            // Generates ETag from the entire response Content
            var etag = ETagService.ComputeWithHashFunction(objectResult.Value);

            if (request.Headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.IfNoneMatch, out var ifNoneMatchValue))
            {
                // Fetch etag from the incoming request header
                var incomingEtag = ifNoneMatchValue.ToString();

                // If both the etags are equal raise a 304 Not Modified Response
                if (incomingEtag.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Any(e => e == $"\"{etag}\""))
                {
                    executedContext.Result = new StatusCodeResult((int)HttpStatusCode.NotModified);

                    response.ContentLength = 0;
                }
            }

            // Add ETag response header
            response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ETag] = new[] { $"\"{etag}\"" };
        }
    }
}

internal static class ETagService
{
    public static string ComputeWithHashFunction(object value)
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                    /* Note: Solves this issue:
                        ---> MessagePack.FormatterNotRegisteredException: NetTopologySuite.Geometries.Geometry is not registered in resolver: MessagePack.Resolvers.ContractlessStandardResolver
                     */
                    new GeometryFormatter(), // Specific formatter for Geometry
                },
                new IFormatterResolver[]
                {
                    ContractlessStandardResolver.Instance, // Generic fallback resolver for all DTOs
                }
            )
        );
        var bytes = MessagePackSerializer.Serialize(value, options);
        var hashData = SHA256.HashData(bytes);

        return Convert.ToBase64String(hashData);
    }
}

internal class GeometryFormatter : IMessagePackFormatter<Geometry>
{
    public void Serialize(ref MessagePackWriter writer, Geometry value, MessagePackSerializerOptions options)
    {
        writer.Write(value?.IsEmpty == true ? null : value.AsText());
    }

    public Geometry Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var wkt = reader.ReadString();
        var readerWkt = new WKTReader();
        return string.IsNullOrEmpty(wkt) ? null : readerWkt.Read(wkt);
    }
}
