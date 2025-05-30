using System;
using System.Collections.Generic;
using System.Linq;

namespace Roxit.ZGW.Catalogi.Web.Extensions;

public static class ListExtensionMethods
{
    public static void AddUnique<T>(this IList<T> source, T item, Func<T, T, bool> comparer)
    {
        if (!source.Any(s => comparer(s, item)))
        {
            source.Add(item);
        }
    }

    public static void AddRangeUnique<T>(this IList<T> source, IEnumerable<T> items, Func<T, T, bool> comparer)
    {
        foreach (T item in items)
        {
            if (!source.Any(s => comparer(s, item)))
            {
                source.Add(item);
            }
        }
    }
}
