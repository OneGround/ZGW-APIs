using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public static class JObjectFilter
{
    public static JObject FilterObjectByPaths(object obj, IEnumerable<string> includeProps)
    {
        if (obj == null)
            return null;

        var jObj = JObject.FromObject(obj);

        var root = new JObject();

        foreach (var path in includeProps)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            // split pad en maak elk deel lower case voor case-insensitieve matching
            var pathParts = path.Split('.');
            AddPath(jObj, root, pathParts);
        }
        AddPath(jObj, root, ["_expand"]);
        return root;
    }

    private static void AddPath(JToken source, JObject target, string[] pathParts, int index = 0)
    {
        if (index >= pathParts.Length)
            return;

        var currentPart = pathParts[index];

        if (currentPart == "*")
        {
            // wildcard — neem alle properties van deze laag
            if (source is JObject sourceObj)
            {
                foreach (var prop in sourceObj.Properties())
                {
                    if (index == pathParts.Length - 1)
                    {
                        target[prop.Name] = prop.Value.DeepClone();
                    }
                    else
                    {
                        if (prop.Value is JObject nestedObj)
                        {
                            var nestedTarget = target[prop.Name] as JObject ?? new JObject();
                            AddPath(nestedObj, nestedTarget, pathParts, index + 1);
                            target[prop.Name] = nestedTarget;
                        }
                        else if (prop.Value is JArray array)
                        {
                            var newArray = new JArray();
                            foreach (var item in array)
                            {
                                if (item is JObject itemObj)
                                {
                                    var filteredItem = new JObject();
                                    AddPath(itemObj, filteredItem, pathParts, index + 1);
                                    newArray.Add(filteredItem);
                                }
                                else
                                {
                                    newArray.Add(item.DeepClone());
                                }
                            }
                            target[prop.Name] = newArray;
                        }
                    }
                }
            }
        }
        else
        {
            // Case-insensitieve property lookup in bronobject
            var srcProp = FindPropertyCaseInsensitive(source, currentPart);
            if (srcProp == null)
                return;

            var token = srcProp.Value;

            if (index == pathParts.Length - 1)
            {
                target[srcProp.Name] = token.DeepClone();
            }
            else
            {
                if (token is JObject nestedObj)
                {
                    var nestedTarget = target[srcProp.Name] as JObject ?? new JObject();
                    AddPath(nestedObj, nestedTarget, pathParts, index + 1);
                    target[srcProp.Name] = nestedTarget;
                }
                else if (token is JArray array)
                {
                    var newArray = new JArray();
                    foreach (var item in array)
                    {
                        if (item is JObject itemObj)
                        {
                            var filteredItem = new JObject();
                            AddPath(itemObj, filteredItem, pathParts, index + 1);
                            newArray.Add(filteredItem);
                        }
                    }
                    target[srcProp.Name] = newArray;
                }
            }
        }
    }

    /// <summary>
    /// Zoekt een property case-insensitief binnen een JObject.
    /// Retourneert de JProperty met originele naam indien gevonden.
    /// </summary>
    private static JProperty FindPropertyCaseInsensitive(JToken token, string propertyName)
    {
        if (token is JObject obj)
        {
            return obj.Properties().FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }
}
