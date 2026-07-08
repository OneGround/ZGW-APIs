using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OneGround.ZGW.Common.Web.Handlers;

/// <summary>
/// Matches a client_id against a set of glob patterns. Only '*' is a wildcard (matches any run of
/// characters, including none); every other character is literal. Matching is whole-string
/// (anchored) and case-insensitive. Used to exclude specific clients from retrieve audit logging.
/// </summary>
public sealed class ClientIdExcludeMatcher
{
    private readonly Regex[] _patterns;

    public ClientIdExcludeMatcher(IEnumerable<string> globPatterns)
    {
        _patterns = (globPatterns ?? Enumerable.Empty<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(ToRegex)
            .ToArray();
    }

    public bool IsExcluded(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            return false;

        return _patterns.Any(r => r.IsMatch(clientId));
    }

    private static Regex ToRegex(string glob)
    {
        // Escape everything literally, then re-open only '*' (escaped by Regex.Escape as "\*") as ".*".
        var escaped = Regex.Escape(glob.Trim()).Replace("\\*", ".*");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
