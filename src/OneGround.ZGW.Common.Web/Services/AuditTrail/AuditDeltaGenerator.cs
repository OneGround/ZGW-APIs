using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

internal static class AuditDeltaGenerator
{
    public static JsonObject GenerateDelta<T>(T original, T current)
    {
        var originalNode = JsonNode.Parse(JsonSerializer.Serialize(original))!.AsObject();
        var currentNode = JsonNode.Parse(JsonSerializer.Serialize(current))!.AsObject();

        return CompareObjects(originalNode, currentNode);
    }

    private static JsonObject CompareObjects(JsonObject original, JsonObject current)
    {
        var delta = new JsonObject();

        foreach (var kv in current)
        {
            original.TryGetPropertyValue(kv.Key, out var oldValue);

            if (kv.Value is JsonObject newObj && oldValue is JsonObject oldObj)
            {
                var child = CompareObjects(oldObj, newObj);
                if (child.Count > 0)
                    delta[kv.Key] = child;
            }
            else if (kv.Value is JsonArray newArr && oldValue is JsonArray oldArr)
            {
                var arrDelta = CompareArrays(oldArr, newArr);
                if (arrDelta.Count > 0)
                    delta[kv.Key] = arrDelta;
            }
            else if (kv.Value != null)
            {
                if (!JsonEquals(oldValue, kv.Value))
                    delta[kv.Key] = kv.Value.DeepClone();
            }
            else if (kv.Value == null && oldValue != null)
            {
                delta[kv.Key] = null;
            }
        }

        return delta;
    }

    private static JsonObject CompareArrays(JsonArray original, JsonArray current)
    {
        var result = new JsonObject();

        var added = new JsonArray();
        var removed = new JsonArray();
        var updated = new JsonArray();

        var hasObjects = original.Any(x => x is JsonObject) || current.Any(x => x is JsonObject);

        if (hasObjects)
        {
            var originalDict = original.OfType<JsonObject>().ToDictionary(GetIdOrHash);

            var currentDict = current.OfType<JsonObject>().ToDictionary(GetIdOrHash);

            foreach (var key in originalDict.Keys)
            {
                if (!currentDict.ContainsKey(key))
                    removed.Add(originalDict[key].DeepClone());
            }

            foreach (var key in currentDict.Keys)
            {
                if (!originalDict.ContainsKey(key))
                    added.Add(currentDict[key].DeepClone());
            }

            foreach (var key in originalDict.Keys)
            {
                if (!currentDict.ContainsKey(key))
                    continue;

                var delta = CompareObjects(originalDict[key], currentDict[key]);

                if (delta.Count > 0)
                {
                    if (currentDict[key].TryGetPropertyValue("Id", out var id))
                        delta["Id"] = id!.DeepClone();

                    updated.Add(delta);
                }
            }
        }
        else
        {
            // For primitive arrays, track counts to handle duplicates
            var originalCounts = original.Select(x => x?.ToJsonString()).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            var currentCounts = current.Select(x => x?.ToJsonString()).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            // Find removed items (including duplicates)
            foreach (var kvp in originalCounts)
            {
                var originalCount = kvp.Value;
                var currentCount = currentCounts.TryGetValue(kvp.Key, out var cc) ? cc : 0;

                if (currentCount < originalCount)
                {
                    var item = original.First(x => x?.ToJsonString() == kvp.Key);
                    for (int i = 0; i < originalCount - currentCount; i++)
                    {
                        removed.Add(item!.DeepClone());
                    }
                }
            }

            // Find added items (including duplicates)
            foreach (var kvp in currentCounts)
            {
                var currentCount = kvp.Value;
                var originalCount = originalCounts.TryGetValue(kvp.Key, out var oc) ? oc : 0;

                if (currentCount > originalCount)
                {
                    var item = current.First(x => x?.ToJsonString() == kvp.Key);
                    for (int i = 0; i < currentCount - originalCount; i++)
                    {
                        added.Add(item!.DeepClone());
                    }
                }
            }
        }

        if (added.Count > 0)
            result["added"] = added;
        if (removed.Count > 0)
            result["removed"] = removed;
        if (updated.Count > 0)
            result["updated"] = updated;

        return result;
    }

    private static string GetIdOrHash(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("Id", out var id))
            return id!.ToJsonString();

        return obj.ToJsonString();
    }

    private static bool JsonEquals(JsonNode a, JsonNode b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;

        return a.ToJsonString() == b.ToJsonString();
    }
}
