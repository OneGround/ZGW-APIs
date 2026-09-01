using System;
using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public class ExpandValidator<TEntity>
{
    private readonly HashSet<string> _allowedPaths;
    private readonly Dictionary<string, string> _parentOf;

    public ExpandValidator(IEnumerable<IExpandResolver<TEntity>> resolvers)
    {
        _allowedPaths = new HashSet<string>(StringComparer.Ordinal);
        _parentOf = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var r in resolvers)
        {
            _allowedPaths.Add(r.Path);
            if (r.Parent != null)
                _parentOf[r.Path] = r.Parent;

            foreach (var (addPath, addParent) in r.AdditionalPaths)
            {
                _allowedPaths.Add(addPath);
                if (addParent != null)
                    _parentOf[addPath] = addParent;
            }
        }
    }

    public (List<string> Paths, string? Error) ParseAndValidate(string? expand)
    {
        if (string.IsNullOrWhiteSpace(expand))
            return ([], null);

        var tokens = expand.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToList();

        var unknown = tokens.Where(t => !_allowedPaths.Contains(t)).ToList();
        if (unknown.Count > 0)
        {
            var unknownList = string.Join(", ", unknown);
            var allowedList = string.Join(", ", _allowedPaths.OrderBy(v => v));
            return ([], $"Ongeldige expand waarde(n): {unknownList}. Toegestane waarden: {allowedList}.");
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            paths.Add(token);
            var current = token;
            while (_parentOf.TryGetValue(current, out var parent))
            {
                paths.Add(parent);
                current = parent;
            }
        }

        return (paths.ToList(), null);
    }
}
