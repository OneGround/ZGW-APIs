using System.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public static class ExpandParserExtensions
{
    public static bool IsValid(this ExpandParser parser, string rootName)
    {
        return parser.Items.Any()
            && !(parser.Items.Count == 1 && parser.Items.ContainsKey(rootName) && !parser.Items[rootName].Any(i => !string.IsNullOrEmpty(i)));
    }
}
