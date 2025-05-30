using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Documenten.Services.Helpers;

namespace Roxit.ZGW.Documenten.Web.Middleware;

public class InterceptBase64ContentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InterceptBase64ContentMiddleware> _logger;

    public InterceptBase64ContentMiddleware(RequestDelegate next, ILogger<InterceptBase64ContentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Note: We intercept (large) POSTED streamed documents before it is buffered. The content is base64 encoded and can be 4 GB in size!
        if (IsDocumentUpload(context))
        {
            _logger.LogDebug("Upload started");

            var started = DateTime.Now;

            // Temporary file will be created (to write the request body inhoud field to it)
            string contentFile = TempFileHelper.GetTempFileName("content.bin");

            _logger.LogDebug("Creating temporary file '{contentFile}' with base64 encoded content", contentFile);

            MemoryStream injectedRequestStream = null;

            try
            {
                // 1. Split base64 encoded .inhoud from request.Body and base64-decode on the fly to content stream (use temporary file). Inject .inhoud to location of this file
                using (var decodedBase64Stream = File.Create(contentFile))
                {
                    using var splitter = new RequestWithBase64ContentSplitter(context.Request.Body);
                    injectedRequestStream = await splitter.RunAsync(decodedBase64Stream, contentFile);

                    if (splitter.Base64DecodingError)
                    {
                        // Note: we want to know there is a base64 decoding error simply by creating an empty error file
                        File.WriteAllText($"{contentFile}.error", "");
                    }
                }

                // 2. Inject the request.Body with the modified request (thus without inhoud)
                injectedRequestStream.Position = 0;

                context.Request.Body = injectedRequestStream;

                // 3. Call the next delegate/middleware in the pipeline
                await _next.Invoke(context);

                _logger.LogDebug("Upload finished in {time}", (DateTime.Now - started).TotalMilliseconds.ToReadableTime());
            }
            finally
            {
                // 4. Free resources
                injectedRequestStream?.Dispose();

                if (File.Exists(contentFile))
                    File.Delete(contentFile);

                if (File.Exists($"{contentFile}.error"))
                    File.Delete($"{contentFile}.error");
            }
        }
        else
        {
            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }

    // Note:
    //  Detemine weather it is a new document upload or an upload of an existing document.
    //  Filter out all other actions such as document-lock/-unlock.
    //
    // Add scenario (so it will return true):
    //  HTTP POST  http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten
    // Update scenario (so it will return true):
    //  HTTP PUT   http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/a5b81602-a9b0-425b-86dc-c304931099cc
    //  HTTP PATCH http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/a5b81602-a9b0-425b-86dc-c304931099cc
    // Lock/Unlock scenario's (so it will return false):
    //  HTTP POST  http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/9051508e-3285-4a04-9d07-b9c063e27864/lock
    //  HTTP POST  http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/9051508e-3285-4a04-9d07-b9c063e27864/unlock

    private static bool IsDocumentUpload(HttpContext context)
    {
        var path = context.Request.Path.ToUriComponent().TrimEnd('/');

        if (path.Contains("/enkelvoudiginformatieobjecten", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                if (path.EndsWith("/enkelvoudiginformatieobjecten")) // Add new EnkelvoudigInformatieObject
                {
                    return true;
                }
            }
            else if (
                context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                || context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
            )
            {
                if (Guid.TryParse(path.Split('/').Last(), out _)) // Modify an existing EnkelvoudigInformatieObject
                {
                    return true;
                }
            }
        }
        return false;
    }
}
