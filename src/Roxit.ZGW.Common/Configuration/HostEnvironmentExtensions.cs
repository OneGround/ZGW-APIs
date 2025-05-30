using System;
using Microsoft.Extensions.Hosting;

namespace Roxit.ZGW.Common.Configuration;

public static class HostEnvironmentExtensions
{
    public static bool IsLocal(this IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        return hostEnvironment.IsEnvironment("Local");
    }

    public static bool IsInternal(this IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.IsLocal() || hostEnvironment.IsDevelopment();
    }
}
