using System.Reflection;

namespace OneGround.ZGW.Common.Extensions;

public static class ApplicationInformation
{
    private static string _cachedName;
    private static string _cachedApplicationVersion;

    /// <summary>
    /// Get version of the application
    /// </summary>
    /// <returns>String of application version</returns>
    public static string GetVersion()
    {
        if (_cachedApplicationVersion != null)
        {
            return _cachedApplicationVersion;
        }

        var assembly = GetAssembly();
        _cachedApplicationVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version.ToVersionString()
            ?? "1.0.0-localdev";

        return _cachedApplicationVersion;
    }

    /// <summary>
    /// Get name of the application
    /// </summary>
    /// <returns>String of application name</returns>
    public static string GetName()
    {
        if (_cachedName != null)
        {
            return _cachedName;
        }

        var assembly = GetAssembly();
        _cachedName = assembly.GetName().Name ?? "unknown-assembly";

        return _cachedName;
    }

    private static Assembly GetAssembly()
    {
        return Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    }
}
