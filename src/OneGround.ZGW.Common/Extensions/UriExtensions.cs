using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.Common.Extensions;

public static class UriExtensions
{
    public static Uri Combine(this Uri baseUri, string resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var uri = $"{baseUri}".TrimEnd('/') + $"/{resource.TrimStart('/')}";

        return Uri.IsWellFormedUriString(uri, UriKind.Relative) ? new Uri(uri, UriKind.Relative) : new Uri(uri);
    }

    public static Uri AddQueryParameter<T>(this Uri uri, string queryParameter, T value)
    {
        ArgumentNullException.ThrowIfNull(queryParameter);

        if (value == null)
            return uri;

        var valueAsString = value.ToString();

        if (valueAsString == null)
            return uri;

        if (value is string)
            valueAsString = Uri.EscapeDataString(valueAsString);

        string uriWithQp;
        if (!uri.ToString().Contains('?'))
            uriWithQp = $"{uri}".TrimEnd('/') + $"?{queryParameter}={valueAsString}";
        else
            uriWithQp = $"{uri}&{queryParameter}={valueAsString}";

        return Uri.IsWellFormedUriString(uriWithQp, UriKind.Relative) ? new Uri(uriWithQp, UriKind.Relative) : new Uri(uriWithQp);
    }

    public static Uri EnsureOneTrailingSlash(this Uri uri)
    {
        if (uri == null)
            return null;

        if (!string.IsNullOrWhiteSpace(uri.Query))
            return uri;

        var trimmedUri = uri.AbsoluteUri.TrimEnd('/') + "/";
        var uriBuilder = new UriBuilder(trimmedUri);
        return uriBuilder.Uri;
    }

    /// <summary>
    /// Get the top level domain of an URI
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException">Thrown when either the input is null</exception>
    /// <returns></returns>
    private static string GetTopLevelDomain(this Uri uri, ILogger logger = null)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            logger?.LogWarning("Failed to determine top level domain from empty uri host. Provided absolute uri: {AbsoluteUri}.", uri.AbsoluteUri);
            return string.Empty;
        }

        var splittedHost = uri.Host.Split('.');

        if (splittedHost.Length != 0)
        {
            if (splittedHost.Length > 1)
            {
                return splittedHost[^2] + "." + splittedHost[^1];
            }

            return splittedHost[^1];
        }

        logger?.LogWarning("Failed to determine top level domain of uri {AbsoluteUri}.", uri.AbsoluteUri);
        return string.Empty;
    }

    /// <summary>
    /// Replace the top level domain of an URI with the current http request top level domain
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException">Thrown when either the input or http context accessor is null</exception>
    /// <returns></returns>
    public static Uri ReplaceTopLevelDomainWithCurrentRequest(
        this Uri input,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger,
        List<string> topLevelDomainWhitelist
    )
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        var httpRequest = httpContextAccessor.HttpContext?.Request;
        if (httpRequest != null)
        {
            return input.ReplaceTopLevelDomainWithCurrentRequest(httpRequest, logger, topLevelDomainWhitelist);
        }

        logger.LogWarning(
            "Unable to determine if top level domain of '{AbsoluteUri}' should be replaced because incomming http request is null.",
            input.AbsoluteUri
        );
        return input;
    }

    /// <summary>
    /// Replace the top level domain of an URI with the passed http request's top level domain
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException">Thrown when either the input or http context accessor is null</exception>
    /// <returns></returns>
    private static Uri ReplaceTopLevelDomainWithCurrentRequest(
        this Uri input,
        HttpRequest httpRequest,
        ILogger logger,
        List<string> topLevelDomainWhitelist
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        if (topLevelDomainWhitelist == null)
        {
            logger.LogDebug("Top level domain whitelist is null.");
            return input;
        }

        if (httpRequest == null)
        {
            logger.LogWarning(
                "Unable to determine if top level domain of '{AbsoluteUri}' should be replaced because incoming http request is null.",
                input.AbsoluteUri
            );
            return input;
        }

        var inputTopLevelDomain = input.GetTopLevelDomain(logger);
        if (string.IsNullOrWhiteSpace(inputTopLevelDomain))
        {
            logger.LogWarning(
                "Unable to determine if top level domain of input URI '{AbsoluteUri}' should be replaced because it's top level domain could not be determined.",
                input.AbsoluteUri
            );
            return input;
        }

        if (!topLevelDomainWhitelist.Any(x => x.Equals(inputTopLevelDomain, StringComparison.CurrentCultureIgnoreCase)))
        {
            return input;
        }

        if (!httpRequest.Host.HasValue)
        {
            logger.LogWarning(
                "Unable to replace top level domain of '{AbsoluteUri}' because the host property of the incoming http request has no value.",
                input.AbsoluteUri
            );
            return input;
        }

        var requestHostUri = httpRequest.Host.Value.StartsWith(httpRequest.Scheme, StringComparison.OrdinalIgnoreCase)
            ? httpRequest.Host.Value
            : $"{httpRequest.Scheme}://{httpRequest.Host}";

        var isCurrentRequestValidUri = Uri.TryCreate(requestHostUri, UriKind.Absolute, out var currentRequestUri);
        if (!isCurrentRequestValidUri)
        {
            logger.LogWarning(
                "Unable to replace top level domain of '{AbsoluteUri}' because incoming http request host '{RequestHostUri}' could not be parsed to a valid URI.",
                input.AbsoluteUri,
                requestHostUri
            );
            return input;
        }

        var currentRequestTopLevelDomain = currentRequestUri.GetTopLevelDomain(logger);
        if (string.IsNullOrWhiteSpace(currentRequestTopLevelDomain))
        {
            logger.LogWarning(
                "Unable to replace top level domain of '{AbsoluteUri}' because top level domain of incoming http request '{CurrentAbsoluteUri}' could not be determined.",
                input.AbsoluteUri,
                currentRequestUri.AbsoluteUri
            );
            return input;
        }

        if (inputTopLevelDomain.Equals(currentRequestTopLevelDomain, StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        logger.LogDebug("Replacing top level domain of '{AbsoluteUri}' with top level domain of current request.", input.AbsoluteUri);

        var replacedHost = input.Host.ToLower().Replace(inputTopLevelDomain.ToLower(), currentRequestTopLevelDomain.ToLower());
        var uriBuilder = new UriBuilder(input.Scheme, replacedHost, input.Port, input.PathAndQuery);

        logger.LogDebug("Replaced top level domain of: '{AbsoluteUri}' to '{BuilderAbsoluteUri}'", input.AbsoluteUri, uriBuilder.Uri.AbsoluteUri);

        return uriBuilder.Uri;
    }
}
