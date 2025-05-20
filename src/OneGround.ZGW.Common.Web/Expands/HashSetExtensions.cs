using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public static class HashSetExtensions
{
    public static bool ContainsIgnoreCase(this HashSet<string> hashset, string key)
    {
        return hashset.Contains(key, new StringEqualityComparer());
    }

    public static bool ContainsAnyOf(this HashSet<string> hashset, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (hashset.Contains(key, new StringEqualityComparer()))
                return true;
        }
        return false;
    }

    public static bool EndsOfAnyOf(this HashSet<string> hashset, params string[] keys)
    {
        var list = hashset.ToList();
        foreach (var key in keys)
        {
            if (hashset.Contains(key, new StringEqualityComparer()))
                return true;

            foreach (string endWith in list)
            {
                if (endWith.EndsWith(key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    public static bool StartsOfAnyOf(this HashSet<string> hashset, params string[] keys)
    {
        var list = hashset.ToList();
        foreach (var key in keys)
        {
            if (hashset.Contains(key, new StringEqualityComparer()))
                return true;

            foreach (string startsWith in list)
            {
                if (startsWith.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }
}

class StringEqualityComparer : IEqualityComparer<string>
{
    public bool Equals(string x, string y)
    {
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode([DisallowNull] string obj)
    {
        return obj.GetHashCode();
    }
}
