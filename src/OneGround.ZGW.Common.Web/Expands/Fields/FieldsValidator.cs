using System;
using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

/// <summary>
/// Validates a parsed <see cref="FieldSelection"/> for <typeparamref name="TEntity"/>:
/// <list type="bullet">
/// <item>every requested <b>scalar</b> field name must exist in the corresponding DTO (per level);</item>
/// <item>every requested <b>nested sub-entity</b> must be an expandable path, i.e. a registered expand
/// resolver must exist for it, so the selection can actually be filled.</item>
/// </list>
/// Unknown field names, non-expandable nesting or unknown sub-entities result in an error.
/// </summary>
public class FieldsValidator<TEntity>
{
    // Limits the recursion depth of Walk so that a pathologically nested FieldSelection (from whatever
    // source) cannot cause unbounded recursion (and eventually a StackOverflowException).
    private const int MaxDepth = 32;

    private readonly FieldsSchema _schema;
    private readonly HashSet<string> _expandablePaths;

    public FieldsValidator(FieldsSchema schema, IEnumerable<string> expandablePaths)
    {
        _schema = schema;
        _expandablePaths = new HashSet<string>(expandablePaths, StringComparer.Ordinal);
    }

    /// <summary>
    /// Checks all field names in <paramref name="selection"/> against the schema and the expandable paths,
    /// and returns every invalid field name as a separate (dotted) path. An empty list means that
    /// everything is valid. The result is sorted for a predictable order.
    /// </summary>
    public IReadOnlyList<string> Validate(FieldSelection selection)
    {
        if (selection is null)
            return [];

        var invalid = new List<string>();
        Walk(selection, [typeof(TEntity)], prefix: "", invalid, depth: 0);
        invalid.Sort(StringComparer.Ordinal);
        return invalid;
    }

    // Works on a list of types so that a polymorphic inline object (e.g. betrokkeneIdentificatie)
    // can be validated against the union of its variants. At concrete levels this is a single type.
    private void Walk(FieldSelection selection, IReadOnlyList<Type> types, string prefix, List<string> invalid, int depth)
    {
        if (depth > MaxDepth)
        {
            invalid.Add(prefix);
            return;
        }

        foreach (var scalar in selection.ScalarFields)
        {
            if (!types.Any(t => _schema.ScalarsOf(t).Contains(scalar)))
                invalid.Add(Combine(prefix, scalar));
        }

        foreach (var (name, child) in selection.Entities)
        {
            var path = Combine(prefix, name);

            // An opaque expand (unknown shape, for instance external JSON) can only be requested as a whole
            if (types.Any(t => _schema.IsOpaqueEntity(t, name)))
            {
                if (!_expandablePaths.Contains(path))
                    invalid.Add(path);
                else if (HasSubSelection(child))
                    invalid.Add($"{path} (veldselectie wordt niet ondersteund)");

                continue;
            }

            // A nested sub-entity is only valid if it is also expandable (a resolver exists for it)
            // and the schema knows the underlying DTO type to validate the deeper fields against.
            Type childType = null;
            foreach (var t in types)
            {
                if (_schema.TryGetEntityType(t, name, out var ct))
                {
                    childType = ct;
                    break;
                }
            }

            if (!_expandablePaths.Contains(path) || childType is null)
            {
                invalid.Add(path);
                continue;
            }

            Walk(child, [childType], path, invalid, depth + 1);
        }

        foreach (var (name, child) in selection.NestedObjects)
        {
            var path = Combine(prefix, name);

            // An inline nested object descends into the underlying DTO type(s); no expand needed.
            var childTypes = new List<Type>();
            foreach (var t in types)
            {
                if (_schema.TryGetNestedTypes(t, name, out var cts))
                    childTypes.AddRange(cts);
            }

            if (childTypes.Count == 0)
            {
                invalid.Add(path);
                continue;
            }

            Walk(child, childTypes, path, invalid, depth + 1);
        }
    }

    // Note: "*" (all fields) is allowed: for an opaque expand that is what happens anyway
    private static bool HasSubSelection(FieldSelection selection) =>
        selection.ScalarFields.Count > 0 || selection.Entities.Count > 0 || selection.NestedObjects.Count > 0;

    private static string Combine(string prefix, string name) => string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
}
