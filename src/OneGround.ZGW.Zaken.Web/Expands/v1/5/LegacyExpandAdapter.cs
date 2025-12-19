using System.Collections.Generic;
using System.IO;
using System.Linq;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

/// <summary>
///   Converts the old way of specifying expands into the new way by expanding each path segment with field selector:
///      (select all fields with "*" because v1.5 does not support field selection which 1.6 does)
///
///      So for example the old Expand "zaaktype.catalogus" will be translated into the new 1.6 format:
///        {
///            "id": "85f2a7a1-f042-4951-a2e5-bb8c8f3ad11b",
///            "fields": [
///                "*",
///                {
///                    "zaaktype": [
///                        "*",
///                        {
///                            "catalogus": [
///                                "*"
///                            ]
///                        }
///                    ]
///                }
///             ]
///        }
/// </summary>
public class LegacyExpandAdapter : IExpandParser
{
    private readonly HashSet<string> _expands;
    private string _rootName;

    public LegacyExpandAdapter(HashSet<string> expands, string rootName)
    {
        // In this adapter we should configure each possible path and fill in the field selector with '*'
        _expands = ExpandPaths(expands);
        _rootName = rootName;
    }

    public HashSet<string> Expands => _expands;

    public string ExpandsString => string.Join(',', _expands);

    public Dictionary<string, HashSet<string>> Items
    {
        get
        {
            // Add the root path
            var items = new Dictionary<string, HashSet<string>>
            {
                {
                    _rootName,
                    new HashSet<string> { "*" }
                },
            };

            // Append with the _expands
            foreach (var e in _expands)
            {
                items[e] = new HashSet<string> { "*" };
            }

            return items;
        }
    }

    private static HashSet<string> ExpandPaths(HashSet<string> input)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();

        foreach (var full in input)
        {
            var parts = full.Split('.');
            string current = "";

            for (int i = 0; i < parts.Length; i++)
            {
                current = (i == 0) ? parts[i] : $"{current}.{parts[i]}";

                if (!seen.Contains(current))
                {
                    seen.Add(current);
                    result.Add(current);
                }
            }
        }

        return result.ToHashSet();
    }
}
