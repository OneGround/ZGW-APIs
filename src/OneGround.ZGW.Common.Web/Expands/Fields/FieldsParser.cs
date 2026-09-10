using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

public static class FieldsParser
{
    // Begrenst de nesting-diepte van een veldpad zodat een diep gepunt pad geen onbeperkte
    // recursie (en uiteindelijk een StackOverflow) in parser/validator/projector kan veroorzaken.
    private const int MaxPathDepth = 20;

    public static (FieldSelection Selection, List<string> ExpandPaths, string Error) ParseAndValidate(JToken fieldsToken)
    {
        if (fieldsToken is null)
            return (null, [], null);

        if (fieldsToken.Type != JTokenType.Array)
            return (null, [], "Het 'fields' veld moet een JSON array zijn.");

        var (selection, error) = ParseArray((JArray)fieldsToken, prefix: "", depth: 0);
        if (error is not null)
            return (null, [], error);

        var expandPaths = new List<string>();
        CollectExpandPaths(selection!, prefix: "", expandPaths);

        return (selection, expandPaths, null);
    }

    // Begrenst ook de nesting-diepte van geneste `{ "naam": [...] }`-objecten, om dezelfde reden als
    // MaxPathDepth hierboven: zonder limiet kan een kwaadwillend diep-geneste 'fields' JSON-array
    // onbeperkte recursie (en een StackOverflow) veroorzaken, los van de lengte van een los veldpad.
    private static (FieldSelection Result, string Error) ParseArray(JArray array, string prefix, int depth)
    {
        if (depth > MaxPathDepth)
            return (null, NestingDepthError(prefix));

        var selection = new FieldSelection();

        foreach (var element in array)
        {
            if (element.Type == JTokenType.String)
            {
                var value = element.Value<string>()!;
                var pathError = AddPath(selection, value, prefix);
                if (pathError is not null)
                    return (null, pathError);
            }
            else if (element.Type == JTokenType.Object)
            {
                var obj = (JObject)element;
                foreach (var prop in obj.Properties())
                {
                    if (prop.Value.Type != JTokenType.Array)
                        return (null, $"De waarde van '{prop.Name}' in 'fields' moet een JSON array zijn.");

                    var childPrefix = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    var (child, error) = ParseArray((JArray)prop.Value, childPrefix, depth + 1);
                    if (error is not null)
                        return (null, error);

                    if (selection.Entities.TryGetValue(prop.Name, out var existing))
                        Merge(existing, child!);
                    else
                        selection.Entities[prop.Name] = child!;
                }
            }
            else
            {
                var location = string.IsNullOrEmpty(prefix) ? "'fields'" : $"'{prefix}'";
                return (null, $"Elk element in {location} moet een veldnaam (string) of een sub-entiteit (object) zijn.");
            }
        }

        return (selection, null);
    }

    /// <summary>
    /// Voegt een (mogelijk gepunt) veldpad toe aan <paramref name="selection"/>. Een pad zonder
    /// punt is een scalair veld (of <c>*</c> voor alle scalairen); een pad met punt daalt af in een
    /// inline genest object (<see cref="FieldSelection.NestedObjects"/>) en werkt zo recursief voor
    /// <c>a.b.c</c> en <c>verlenging.*</c>. Retourneert een foutmelding bij een leeg segment, anders <c>null</c>.
    /// </summary>
    private static string AddPath(FieldSelection selection, string path, string prefix)
    {
        var segments = path.Split('.');
        if (segments.Length > MaxPathDepth)
            return PathError(path, prefix);

        var current = selection;
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0)
                return PathError(path, prefix);

            if (i == segments.Length - 1)
            {
                if (segment == "*")
                    current.IncludeAllScalars = true;
                else
                    current.ScalarFields.Add(segment);
            }
            else
            {
                if (!current.NestedObjects.TryGetValue(segment, out var child))
                    current.NestedObjects[segment] = child = new FieldSelection();
                current = child;
            }
        }

        return null;
    }

    private static string PathError(string path, string prefix)
    {
        var location = string.IsNullOrEmpty(prefix) ? "'fields'" : $"'{prefix}'";
        return $"Ongeldig veldpad in {location}: '{path}'.";
    }

    private static string NestingDepthError(string prefix)
    {
        var location = string.IsNullOrEmpty(prefix) ? "'fields'" : $"'{prefix}'";
        return $"Te diep geneste 'fields' selectie rond {location} (maximaal {MaxPathDepth} niveaus).";
    }

    private static void Merge(FieldSelection target, FieldSelection source)
    {
        foreach (var scalar in source.ScalarFields)
            target.ScalarFields.Add(scalar);

        if (source.IncludeAllScalars)
            target.IncludeAllScalars = true;

        foreach (var (key, childSource) in source.Entities)
        {
            if (target.Entities.TryGetValue(key, out var childTarget))
                Merge(childTarget, childSource);
            else
                target.Entities[key] = childSource;
        }

        foreach (var (key, childSource) in source.NestedObjects)
        {
            if (target.NestedObjects.TryGetValue(key, out var childTarget))
                Merge(childTarget, childSource);
            else
                target.NestedObjects[key] = childSource;
        }
    }

    private static void CollectExpandPaths(FieldSelection selection, string prefix, List<string> paths)
    {
        foreach (var (key, child) in selection.Entities)
        {
            var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
            paths.Add(path);
            CollectExpandPaths(child, path, paths);
        }
    }
}
