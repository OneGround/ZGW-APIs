using System;

namespace OneGround.ZGW.Common.ServiceAgent.Expands;

/// <summary>
/// An expand could not be resolved because another service (DRC, ZTC, ...) could not be reached or answered with an error.
/// <para>
/// <see cref="ServiceUrl"/> is the resource that was asked of that service. <see cref="StatusCode"/> is the HTTP status it answered with
/// (null when it did not answer at all, e.g. a timeout; <see cref="Exception.InnerException"/> then holds the cause). The message (which
/// also contains the reason the service agent recorded) is meant for the log: the controllers only give the client the service name, the
/// url and the status.
/// </para>
/// </summary>
public sealed class ExpandExternalServiceException : Exception
{
    public string ServiceName { get; }
    public string ServiceUrl { get; }
    public int? StatusCode { get; }

    public ExpandExternalServiceException(string serviceName, string serviceUrl, Exception inner)
        : this(serviceName, serviceUrl, null, null, inner) { }

    private ExpandExternalServiceException(string serviceName, string serviceUrl, int? statusCode, string reason, Exception inner)
        : base(CreateMessage(serviceName, serviceUrl, statusCode, reason), inner)
    {
        ServiceName = serviceName;
        ServiceUrl = serviceUrl;
        StatusCode = statusCode;
    }

    /// <summary>
    /// For a call that did not succeed: keeps the HTTP status, the reason and the exception that the service agent recorded in
    /// <paramref name="response"/>, instead of throwing them away.
    /// </summary>
    public static ExpandExternalServiceException ForFailedResponse(string serviceName, string serviceUrl, ServiceAgentResponse response)
    {
        var error = response?.Error;
        int? statusCode = error is { Status: > 0 } ? error.Status : null;

        return new ExpandExternalServiceException(serviceName, serviceUrl, statusCode, error?.Title ?? error?.Detail, response?.Exception);
    }

    /// <summary>
    /// True when the service answered that the resource is not available to the caller (403 Forbidden or 404 Not Found). The user-authenticated
    /// agents forward the caller's own token, so the service decides as the caller: such a resource is simply not for this caller.
    /// </summary>
    public static bool IsNotAvailableToCaller(ServiceAgentResponse response) => response?.Error?.Status is 403 or 404;

    private static string CreateMessage(string serviceName, string serviceUrl, int? statusCode, string reason)
    {
        var message = $"Externe service '{serviceName}' is niet bereikbaar of heeft een fout teruggegeven voor URL '{serviceUrl}'";

        if (statusCode is not null)
            message += $" (HTTP {statusCode})";

        if (!string.IsNullOrWhiteSpace(reason))
            message += $": {reason}";

        return message + ".";
    }
}
