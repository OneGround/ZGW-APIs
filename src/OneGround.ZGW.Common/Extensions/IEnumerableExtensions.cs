﻿using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.Common.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }
}
