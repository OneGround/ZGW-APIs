using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

public static class FieldProjector
{
    public static Dictionary<string, object> Project<TEntity>(TEntity entity, FieldSelection selection) => ProjectObject(entity!, selection);

    public static List<Dictionary<string, object>> ProjectList<TEntity>(IEnumerable<TEntity> entities, FieldSelection selection) =>
        entities.Select(e => Project(e, selection)).ToList();

    private static Dictionary<string, object> ProjectObject(object source, FieldSelection selection)
    {
        var result = new Dictionary<string, object>();
        var properties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        PropertyInfo expandProperty = null;
        var scalarLookup = new Dictionary<string, PropertyInfo>();

        foreach (var prop in properties)
        {
            var jsonAttr = prop.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonAttr == null)
                continue;

            if (jsonAttr.PropertyName == "_expand")
            {
                expandProperty = prop;
                continue;
            }

            scalarLookup[jsonAttr.PropertyName] = prop;
        }

        if (selection.IncludeAllScalars)
        {
            foreach (var (jsonName, prop) in scalarLookup)
                result[jsonName] = prop.GetValue(source);
        }
        else
        {
            foreach (var fieldName in selection.ScalarFields)
            {
                if (scalarLookup.TryGetValue(fieldName, out var prop))
                    result[fieldName] = prop.GetValue(source);
            }
        }

        // Inline geneste objecten worden ter plekke geprojecteerd (niet via _expand). Scalars zijn
        // hierboven al verwerkt; een geneste selectie op dezelfde naam heeft daarna voorrang.
        foreach (var (name, subSelection) in selection.NestedObjects)
        {
            if (scalarLookup.TryGetValue(name, out var prop))
                result[name] = ProjectNested(prop.GetValue(source), subSelection);
        }

        if (selection.Entities.Count > 0)
        {
            var expandDict = new Dictionary<string, object>();
            var expandValue = expandProperty?.GetValue(source) as Dictionary<string, object>;

            foreach (var (entityName, subSelection) in selection.Entities)
            {
                if (
                    expandValue != null
                    && expandValue.TryGetValue(entityName, out var entityValue)
                    && entityValue != null
                    && entityValue.GetType() != typeof(object)
                )
                {
                    if (entityValue is IEnumerable<object> enumerable)
                        expandDict[entityName] = enumerable.Select(item => ProjectObject(item, subSelection)).ToList();
                    else
                        expandDict[entityName] = ProjectObject(entityValue, subSelection);
                }
                else
                {
                    expandDict[entityName] = new object();
                }
            }

            result["_expand"] = expandDict;
        }

        return result;
    }

    private static object ProjectNested(object value, FieldSelection selection)
    {
        if (value is null)
            return null;

        // Een collectie van geneste objecten: projecteer elk element afzonderlijk.
        if (value is IEnumerable enumerable && value is not string)
        {
            var items = new List<object>();
            foreach (var item in enumerable)
                items.Add(item is null ? null : ProjectObject(item, selection));
            return items;
        }

        return ProjectObject(value, selection);
    }
}
