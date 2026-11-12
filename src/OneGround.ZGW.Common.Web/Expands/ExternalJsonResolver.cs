using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Http;

namespace OneGround.ZGW.Common.Web.Expands;

/// <summary>
/// Base for the top-level expand paths whose value is an optional url that is fetched as a JSON object from an external, unauthenticated API
/// (e.g. the selectielijst of the VNG). Whatever goes wrong (url not allowed, not reachable, not JSON, ...) results in an empty object, see
/// <see cref="IExternalJsonClient"/>. The shape of the external document is unknown to us, so it is stored as is in <c>_expand</c>, and in "fields"
/// these paths are registered as opaque entities: they can be requested as a whole (<c>{ "communicatiekanaal": [] }</c>), never with a field selection.
/// </summary>
/// <typeparam name="TEntity">The response that is expanded.</typeparam>
public abstract class ExternalJsonResolver<TEntity>(IExternalJsonClient externalJsonClient) : IExpandResolver<TEntity>
{
    public abstract string Path { get; }
    public string Parent => null;

    /// <summary>The url in the entity that is to be fetched, or null.</summary>
    protected abstract string GetUrl(TEntity entity);

    public async Task<object> ResolveAsync(TEntity entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        return await externalJsonClient.GetJsonObjectAsync(GetUrl(entity));
    }
}
