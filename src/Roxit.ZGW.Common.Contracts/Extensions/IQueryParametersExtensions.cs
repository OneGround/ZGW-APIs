using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Roxit.ZGW.Common.Contracts.Extensions;

public static class IQueryParametersExtensions
{
    private static readonly ConcurrentDictionary<Type, IEnumerable<CachedQueryParameter>> Cache = new();

    /// <summary>
    /// Returns all [FromQuery] annotated properties in the <see cref="IQueryParameters"/> instance.
    /// </summary>
    /// <param name="queryParameters"></param>
    /// <returns></returns>
    public static IEnumerable<CachedQueryParameter> GetParameters(this IQueryParameters queryParameters)
    {
        return Cache.GetOrAdd(
            queryParameters.GetType(),
            type =>
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => Attribute.IsDefined(p, typeof(FromQueryAttribute)))
                    .Where(p => p.PropertyType == typeof(string))
                    .Select(p => new CachedQueryParameter(p));
            }
        );
    }
}
