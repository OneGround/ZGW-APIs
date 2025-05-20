using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Web.Validations;

public static class PropertyNameResolver
{
    private static readonly ConcurrentDictionary<(Type, MemberInfo), string> _cache = new ConcurrentDictionary<(Type, MemberInfo), string>();

    /// <summary>
    /// Resolves property name from JsonProperty attribute, otherwise returns original property name.
    /// </summary>
    public static string Default(Type type, MemberInfo memberInfo, LambdaExpression _)
    {
        if (memberInfo == null)
            return null;

        return _cache.GetOrAdd(
            (type, memberInfo),
            _ =>
            {
                // for request model properties annotated with [JsonProperty()]
                var jsonPropertyAttribute = memberInfo.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropertyAttribute != null)
                {
                    return jsonPropertyAttribute.PropertyName ?? memberInfo.Name;
                }
                // for request model from querystring annotated with [FromQuery()]
                var fromQueryAttribute = memberInfo.GetCustomAttribute<FromQueryAttribute>();
                if (fromQueryAttribute != null)
                {
                    return fromQueryAttribute.Name ?? memberInfo.Name;
                }

                return memberInfo.Name;
            }
        );
    }
}
