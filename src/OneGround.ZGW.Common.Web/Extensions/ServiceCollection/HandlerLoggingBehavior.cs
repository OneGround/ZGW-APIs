using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection;

public class HandlerLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<TRequest> _logger;

    public HandlerLoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = default(TResponse);
        try
        {
            _logger.LogDebug("Handler started: {@HandlerRequest}", request);

            response = await next(cancellationToken);
        }
        finally
        {
            _logger.LogDebug("Handler finished: {@HandlerResponse} took {ElapsedMilliseconds}ms", response, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
