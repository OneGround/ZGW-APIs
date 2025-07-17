using Microsoft.Extensions.Logging;

namespace KeycloakSetup;

public class BadRequestLoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<BadRequestLoggingDelegatingHandler> _logger;

    public BadRequestLoggingDelegatingHandler(ILogger<BadRequestLoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogTrace("Sending request {RequestMethod} {RequestUri}.", request.Method, request.RequestUri);

            var response = await base.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Bad request response received for {RequestMethod} {RequestUri}: {Content}",
                    request.Method, request.RequestUri, content);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught for request {RequestMethod} {RequestUri}", request.Method, request.RequestUri);
            throw;
        }
    }
}