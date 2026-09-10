using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Expands;

public interface IExpandResolver<TEntity>
{
    string Path { get; }
    string Parent { get; }

    // Extra paden die deze resolver intern afhandelt (voor validatie + parent-implicatie, niet gedispatcht)
    IEnumerable<(string Path, string Parent)> AdditionalPaths => [];

    Task<object> ResolveAsync(TEntity entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths);
}
