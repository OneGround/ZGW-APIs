using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands;

/// <summary>
/// Eenvoudige <see cref="IExpandable"/>-implementatie voor tests. Wordt zowel als de te
/// expanderen entity gebruikt, als als (resolved) parent-waarde om nesting te testen.
/// </summary>
public sealed class FakeExpandable : IExpandable
{
    public Dictionary<string, object?>? Expand { get; set; }
}

/// <summary>
/// Configureerbare fake-resolver. Legt vast hoe vaak en in welke volgorde hij is aangeroepen,
/// en met welke <c>resolved</c>/<c>requestedPaths</c>, zodat topologische sortering en
/// doorgegeven context geverifieerd kunnen worden.
/// </summary>
public sealed class FakeResolver : IExpandResolver<FakeExpandable>
{
    private readonly Func<FakeExpandable, IReadOnlyDictionary<string, object?>, IReadOnlySet<string>, object?> _resolve;

    /// <summary>Gedeelde lijst die de aanroepvolgorde van paden over alle resolvers vastlegt.</summary>
    public List<string>? CallLog { get; set; }

    public FakeResolver(
        string path,
        object? returnValue = null,
        string? parent = null,
        IEnumerable<(string Path, string? Parent)>? additionalPaths = null,
        Func<FakeExpandable, IReadOnlyDictionary<string, object?>, IReadOnlySet<string>, object?>? resolve = null
    )
    {
        Path = path;
        Parent = parent;
        AdditionalPaths = additionalPaths ?? [];
        _resolve = resolve ?? ((_, _, _) => returnValue);
    }

    public string Path { get; }
    public string? Parent { get; }
    public IEnumerable<(string Path, string? Parent)> AdditionalPaths { get; }

    // Vastgelegde context van de laatste aanroep.
    public int CallCount { get; private set; }
    public IReadOnlyDictionary<string, object?>? LastResolved { get; private set; }
    public IReadOnlySet<string>? LastRequestedPaths { get; private set; }

    public Task<object?> ResolveAsync(FakeExpandable entity, IReadOnlyDictionary<string, object?> resolved, IReadOnlySet<string> requestedPaths)
    {
        CallCount++;
        // Snapshot van resolved zodat latere mutaties asserts niet beïnvloeden.
        LastResolved = new Dictionary<string, object?>(resolved, StringComparer.Ordinal);
        LastRequestedPaths = requestedPaths;
        CallLog?.Add(Path);

        return Task.FromResult(_resolve(entity, resolved, requestedPaths));
    }
}
