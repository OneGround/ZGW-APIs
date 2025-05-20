using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace OneGround.ZGW.Common.Batching;

public class BatchIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BatchIdMiddleware> _logger;

    public BatchIdMiddleware(RequestDelegate next, ILogger<BatchIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IBatchIdAccessor batchIdAccessor)
    {
        if (context.Request.Headers.TryGetValue("X-Batch-Id", out var batchIdValues) && !StringValues.IsNullOrEmpty(batchIdValues))
        {
            var batchId = batchIdValues.FirstOrDefault();
            batchIdAccessor.Id = batchId;
            _logger.LogDebug("BatchId ({batchId}) found and set to batch Id accessor.", batchId);

            using (_logger.BeginScope(new Dictionary<string, object> { ["BatchId"] = batchId }))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }
}
