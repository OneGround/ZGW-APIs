using System;

namespace OneGround.ZGW.Common.Web.Expands;

public sealed class ExpandExternalServiceException : Exception
{
    public string ServiceName { get; }
    public string ServiceUrl { get; }

    public ExpandExternalServiceException(string serviceName, string serviceUrl, Exception inner)
        : base($"Externe service '{serviceName}' is niet bereikbaar of heeft een fout teruggegeven voor URL '{serviceUrl}'.", inner)
    {
        ServiceName = serviceName;
        ServiceUrl = serviceUrl;
    }
}
