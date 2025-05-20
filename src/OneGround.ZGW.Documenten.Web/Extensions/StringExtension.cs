using System.Text.RegularExpressions;

namespace OneGround.ZGW.Documenten.Web.Extensions;

public static class StringExtension
{
    public static int RegexIndexOf(this string inputString, string pattern)
    {
        var m = Regex.Match(inputString, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Index : -1;
    }

    public static string RegexReplace(this string inputString, string pattern, string replaceString)
    {
        string value = Regex.Replace(inputString, pattern, replaceString, RegexOptions.IgnoreCase);
        return value;
    }
}
