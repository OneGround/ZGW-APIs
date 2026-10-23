using System;

namespace OneGround.ZGW.Common.Web.Configuration;

/// <summary>
/// Settings for <see cref="Http.IExternalJsonClient"/>, which fetches JSON from urls that are supplied by API clients (and are
/// therefore untrusted). Secure by default: with no <see cref="AllowedHosts"/> configured every request is refused.
/// </summary>
public class ExternalJsonClientSettings
{
    /// <summary>
    /// Hosts that may be called. An entry is either an exact host name (<c>api.example.com</c>) or a wildcard for all subdomains
    /// (<c>*.example.com</c>, which does not match <c>example.com</c> itself). Case-insensitive. Empty (default) refuses everything.
    /// </summary>
    public string[] AllowedHosts { get; set; } = [];

    /// <summary>
    /// When false (default) a host that resolves to a loopback, link-local, private or otherwise non-public address is refused,
    /// which protects internal services against server-side request forgery. Only enable for local development.
    /// </summary>
    public bool AllowPrivateAddresses { get; set; }

    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

    /// <summary>
    /// Total time all external calls of ONE incoming request may take together (a list of zaken would otherwise multiply the
    /// per-call timeout by the number of zaken). Once used up, further calls yield an empty object without calling out.
    /// </summary>
    public int TotalTimeoutSeconds { get; set; } = DefaultTotalTimeoutSeconds;

    public int MaxResponseBytes { get; set; } = DefaultMaxResponseBytes;

    // Note: a misconfigured value (zero, negative, absurdly large) must neither break the client nor switch off its protection, so
    // the values actually used fall back to the default when out of range.
    public const int DefaultTimeoutSeconds = 3;
    public const int DefaultTotalTimeoutSeconds = 10;
    public const int DefaultMaxResponseBytes = 256 * 1024;
    public const int MaxAllowedTimeoutSeconds = 30;
    public const int MaxAllowedTotalTimeoutSeconds = 60;
    public const int MaxAllowedResponseBytes = 4 * 1024 * 1024;

    public TimeSpan TotalTimeout =>
        TimeSpan.FromSeconds(TotalTimeoutSeconds is > 0 and <= MaxAllowedTotalTimeoutSeconds ? TotalTimeoutSeconds : DefaultTotalTimeoutSeconds);

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds is > 0 and <= MaxAllowedTimeoutSeconds ? TimeoutSeconds : DefaultTimeoutSeconds);

    public int EffectiveMaxResponseBytes => MaxResponseBytes is > 0 and <= MaxAllowedResponseBytes ? MaxResponseBytes : DefaultMaxResponseBytes;
}
