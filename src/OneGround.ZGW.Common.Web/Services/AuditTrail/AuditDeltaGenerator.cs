using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

/// <summary>
/// Generates delta objects that capture only the differences between two objects.
/// Uses two special markers for edge cases:
/// - "__removed": Indicates a property was completely removed (distinguishes from "set to null")
/// - "__replace": Indicates the entire value should replace existing value (prevents merging)
/// </summary>
public static class AuditDeltaGenerator
{
    public static JsonObject GenerateDelta<T>(T original, T current, List<string> propertiesUsingCurrentValue = null)
    {
        var originalNode = JsonNode.Parse(JsonSerializer.Serialize(original))!.AsObject();
        var currentNode = JsonNode.Parse(JsonSerializer.Serialize(current))!.AsObject();

        return CompareObjects(originalNode, currentNode, propertiesUsingCurrentValue ?? new List<string>());
    }

    private static JsonObject CompareObjects(JsonObject original, JsonObject current, List<string> propertiesUsingCurrentValue)
    {
        var delta = new JsonObject();

        foreach (var kv in current)
        {
            original.TryGetPropertyValue(kv.Key, out var oldValue);

            if (kv.Value is JsonObject newObj && oldValue is JsonObject oldObj)
            {
                var child = CompareObjects(oldObj, newObj, propertiesUsingCurrentValue);
                if (child.Count > 0)
                {
                    // If this property should use current value instead of delta, always include it
                    // Wrap it in a special marker to indicate it should replace the entire value
                    if (propertiesUsingCurrentValue.Contains(kv.Key))
                    {
                        if (kv.Value != null)
                        {
                            delta[kv.Key] = new JsonObject { ["__replace"] = kv.Value.DeepClone() };
                            continue;
                        }
                    }
                    delta[kv.Key] = child;
                }
            }
            else if (kv.Value is JsonArray newArr && oldValue is JsonArray oldArr)
            {
                var arrDelta = CompareArrays(oldArr, newArr, propertiesUsingCurrentValue);
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

        // Check for properties that exist in original but not in current (removed properties)
        // Use a special marker to distinguish between "removed" and "set to null"
        // => Edge case: find in patching Geometry complex type to a different type, eg. "GeometryCollection" to "Point"
        foreach (var kv in original)
        {
            if (!current.ContainsKey(kv.Key))
            {
                delta[kv.Key] = new JsonObject { ["__removed"] = true };
            }
        }

        return delta;
    }

    private static JsonObject CompareArrays(JsonArray original, JsonArray current, List<string> propertiesUsingCurrentValue)
    {
        var result = new JsonObject();

        var added = new JsonArray();
        var removed = new JsonArray();
        var updated = new JsonArray();

        var hasObjects = original.Any(x => x is JsonObject) || current.Any(x => x is JsonObject);

        if (hasObjects)
        {
            // Group instead of a straight ToDictionary: items without an "Id" fall back to a full-content
            // hash as key (see GetIdOrHash), so duplicate/identical objects in the array (a valid PATCH
            // payload, e.g. two identical "activiteiten" entries) share a key and must not throw on insert.
            var originalGroups = original.OfType<JsonObject>().GroupBy(GetIdOrHash).ToDictionary(g => g.Key, g => g.ToList());
            var currentGroups = current.OfType<JsonObject>().GroupBy(GetIdOrHash).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var key in originalGroups.Keys.Union(currentGroups.Keys))
            {
                var originalItems = originalGroups.TryGetValue(key, out var oItems) ? oItems : new List<JsonObject>();
                var currentItems = currentGroups.TryGetValue(key, out var cItems) ? cItems : new List<JsonObject>();

                var pairCount = Math.Min(originalItems.Count, currentItems.Count);

                for (int i = 0; i < pairCount; i++)
                {
                    var delta = CompareObjects(originalItems[i], currentItems[i], propertiesUsingCurrentValue);

                    if (delta.Count > 0)
                    {
                        if (currentItems[i].TryGetPropertyValue("Id", out var id))
                            delta["Id"] = id!.DeepClone();

                        updated.Add(delta);
                    }
                }

                for (int i = pairCount; i < originalItems.Count; i++)
                    removed.Add(originalItems[i].DeepClone());

                for (int i = pairCount; i < currentItems.Count; i++)
                    added.Add(currentItems[i].DeepClone());
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
        if (obj.TryGetPropertyValue("Id", out var id) && id != null)
            return id.ToJsonString();

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
