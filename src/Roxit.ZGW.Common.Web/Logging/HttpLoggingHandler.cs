using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Roxit.ZGW.Common.Web.Logging;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public HttpLoggingHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using (Log.BeginRequestPipelineScope(_logger, request))
        {
            var start = DateTime.UtcNow;
            var response = await base.SendAsync(request, cancellationToken);
            var duration = DateTime.UtcNow - start;
            await Log.RequestResponseSummary(_logger, request, response, duration);

            return response;
        }
    }

    private static class Log
    {
        private static readonly Func<ILogger, HttpMethod, Uri, IDisposable> _beginRequestPipelineScope = LoggerMessage.DefineScope<HttpMethod, Uri>(
            "HTTP {HttpMethod} {Uri}"
        );

        private static readonly Action<ILogger, int, HttpMethod, Uri, string, Exception> _requestPipelineSummarySuccess = LoggerMessage.Define<
            int,
            HttpMethod,
            Uri,
            string
        >(LogLevel.Information, new EventId(), "Received HTTP Response {StatusCode} from {HttpMethod} {Uri} in {Duration}");

        private static readonly Action<ILogger, int, HttpMethod, Uri, string, string, Exception> _requestPipelineSummaryWarning =
            LoggerMessage.Define<int, HttpMethod, Uri, string, string>(
                LogLevel.Warning,
                new EventId(),
                "Received HTTP Response {StatusCode} from {HttpMethod} {Uri} in {Duration}, contents: {ResponseBody}"
            );

        private static readonly Action<ILogger, int, HttpMethod, Uri, string, string, Exception> _requestPipelineSummaryError = LoggerMessage.Define<
            int,
            HttpMethod,
            Uri,
            string,
            string
        >(LogLevel.Error, new EventId(), "Received HTTP Response {StatusCode} from {HttpMethod} {Uri} in {Duration}, contents: {ResponseBody}");

        public static IDisposable BeginRequestPipelineScope(ILogger logger, HttpRequestMessage request)
        {
            return _beginRequestPipelineScope(logger, request.Method, request.RequestUri);
        }

        public static async Task RequestResponseSummary(ILogger logger, HttpRequestMessage request, HttpResponseMessage response, TimeSpan duration)
        {
            if (response.IsSuccessStatusCode)
            {
                _requestPipelineSummarySuccess(
                    logger,
                    (int)response.StatusCode,
                    request.Method,
                    request.RequestUri,
                    duration.ToHumanTimeString(),
                    null
                );
            }
            else
            {
                var responseBody = response.Content != null ? await response.Content.ReadAsStringAsync() : "<empty>";

                if (response.StatusCode < HttpStatusCode.InternalServerError)
                    _requestPipelineSummaryWarning(
                        logger,
                        (int)response.StatusCode,
                        request.Method,
                        request.RequestUri,
                        duration.ToHumanTimeString(),
                        responseBody,
                        null
                    );
                else
                    _requestPipelineSummaryError(
                        logger,
                        (int)response.StatusCode,
                        request.Method,
                        request.RequestUri,
                        duration.ToHumanTimeString(),
                        responseBody,
                        null
                    );
            }
        }
    }
}
