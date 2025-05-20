using System;

namespace OneGround.ZGW.Common.Services;

public interface IServiceDiscovery
{
    /// <summary>
    /// Retrieves the API endpoint URI for the specified service.
    /// </summary>
    /// <param name="service">The name of the service, e.g., "ZRC".</param>
    /// <returns>A URI object representing the API endpoint.</returns>
    Uri GetApi(string service);

    /// <summary>
    /// Retrieves the API endpoint URI for the specified service and version.
    /// </summary>
    /// <param name="service">The name of the service, e.g., "ZRC".</param>
    /// <param name="version">The version suffix, e.g., "v1".</param>
    /// <returns>A URI object representing the versioned API endpoint.</returns>
    Uri GetApi(string service, string version);
}
