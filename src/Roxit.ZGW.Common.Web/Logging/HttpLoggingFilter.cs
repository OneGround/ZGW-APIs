using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Roxit.ZGW.Common.Web.Logging;

/// <summary>
/// <seealso cref="https://www.stevejgordon.co.uk/httpclientfactory-asp-net-core-logging"/>
/// </summary>
public class HttpLoggingFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly ILoggerFactory _loggerFactory;

    public HttpLoggingFilter(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (builder) =>
        {
            // Run other configuration first, we want to decorate.
            next(builder);

            var outerLogger = _loggerFactory.CreateLogger($"System.Net.Http.HttpClient.{builder.Name}.LogicalHandler");

            builder.AdditionalHandlers.Insert(0, new HttpLoggingHandler(outerLogger));
        };
    }
}
