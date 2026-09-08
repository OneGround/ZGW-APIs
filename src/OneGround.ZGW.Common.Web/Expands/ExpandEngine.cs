using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts;

namespace OneGround.ZGW.Common.Web.Expands;

public class ExpandEngine<TEntity>
    where TEntity : IExpandable
{
    private readonly Dictionary<string, IExpandResolver<TEntity>> _dispatched;
    private readonly Dictionary<string, string> _parentOf;

    public ExpandEngine(IEnumerable<IExpandResolver<TEntity>> resolvers)
    {
        var list = resolvers.ToList();
        _dispatched = list.ToDictionary(r => r.Path, StringComparer.Ordinal);
        _parentOf = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var r in list)
        {
            if (r.Parent != null)
                _parentOf[r.Path] = r.Parent;

            foreach (var (addPath, addParent) in r.AdditionalPaths)
            {
                if (addParent != null)
                    _parentOf[addPath] = addParent;
            }
        }
    }

    public async Task ResolveAsync(TEntity entity, IEnumerable<string> requestedPaths)
    {
        var pathSet = new HashSet<string>(requestedPaths, StringComparer.Ordinal);
        if (pathSet.Count == 0)
            return;

        var ordered = TopologicalSort(pathSet);
        var resolved = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var path in ordered)
        {
            if (!_dispatched.TryGetValue(path, out var resolver))
                continue;

            var result = await resolver.ResolveAsync(entity, resolved, pathSet);
            resolved[path] = result ?? new object();
        }

        // Top-level paden → entity._expand
        var topLevel = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var (path, value) in resolved)
        {
            if (!path.Contains('.'))
            {
                topLevel[path] = value;
            }
            else
            {
                // Genest: injecteer in parent._expand voor IExpandable ouders
                var lastDot = path.LastIndexOf('.');
                var parentPath = path[..lastDot];
                var childKey = path[(lastDot + 1)..];

                if (resolved.TryGetValue(parentPath, out var parentObj) && parentObj is IExpandable expandableParent)
                {
                    expandableParent.Expand ??= new Dictionary<string, object>();
                    expandableParent.Expand[childKey] = value ?? new object();
                }
            }
        }

        entity.Expand = topLevel.Count > 0 ? topLevel : null;
    }

    public async Task ResolveListAsync(IEnumerable<TEntity> entities, IEnumerable<string> requestedPaths)
    {
        var pathList = requestedPaths.ToList();
        if (pathList.Count == 0)
            return;

        foreach (var entity in entities)
            await ResolveAsync(entity, pathList);
    }

    private List<string> TopologicalSort(HashSet<string> paths)
    {
        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        void Visit(string path)
        {
            if (!visited.Add(path))
                return;
            if (_parentOf.TryGetValue(path, out var parent) && paths.Contains(parent))
                Visit(parent);
            result.Add(path);
        }

        foreach (var path in paths)
            Visit(path);

        return result;
    }
}
