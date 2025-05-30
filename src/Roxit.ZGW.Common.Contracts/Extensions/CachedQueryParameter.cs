using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Roxit.ZGW.Common.Contracts.Extensions;

public class CachedQueryParameter
{
    private readonly PropertyInfo _propertyInfo;

    public CachedQueryParameter(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo;

        ParameterName = propertyInfo.Name;
        QueryName = propertyInfo.GetCustomAttribute<FromQueryAttribute>()?.Name;
    }

    public string ParameterName { get; }
    public string QueryName { get; }

    public string GetValue(IQueryParameters queryParameters)
    {
        return (string)_propertyInfo.GetValue(queryParameters);
    }
}
