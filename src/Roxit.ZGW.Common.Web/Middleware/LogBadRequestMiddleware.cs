using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace Roxit.ZGW.Common.Web.Middleware;

public class LogBadRequestMiddleware
{
    private readonly RequestDelegate _requestProcess;
    private readonly ILogger _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public LogBadRequestMiddleware(RequestDelegate requestProcess, ILogger<LogBadRequestMiddleware> logger)
    {
        _requestProcess = requestProcess;
        _logger = logger;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
        {
            // Handle and intercept (log) BadRequests
            await HandleAndLogIfBadRequest(context);
        }
        else
        {
            // Normal flow
            await _requestProcess(context);
        }
    }

    private async Task HandleAndLogIfBadRequest(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        await using var responseBody = _recyclableMemoryStreamManager.GetStream();

        context.Response.Body = responseBody;

        await _requestProcess(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);

        if (context.Response.StatusCode == (int)HttpStatusCode.BadRequest)
        {
            var badRequestResponse = await new StreamReader(context.Response.Body).ReadToEndAsync();

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogWarning(
                "HTTP {Method} {Scheme}://{Host}{Path} resulted in a {BadRequest}:",
                context.Request.Method,
                context.Request.Scheme,
                context.Request.Host,
                context.Request.Path,
                HttpStatusCode.BadRequest
            );
            _logger.LogWarning("Response: {badRequestResponse}", badRequestResponse);
        }

        await responseBody.CopyToAsync(originalBodyStream);
    }
}
