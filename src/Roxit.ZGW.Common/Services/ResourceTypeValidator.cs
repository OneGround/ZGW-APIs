using System.Text.RegularExpressions;

namespace Roxit.ZGW.Common.Services;

public static class ResourceTypeValidator
{
    public static bool IsOfType(string entity, string url)
    {
        var regex = new Regex($@"/(?<entity>{entity})/(?<uuid>[a-z0-9\-]{{36}})", RegexOptions.IgnoreCase);
        return regex.IsMatch(url);
    }
}
