using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Base of the expand resolvers whose value is an optional url that is fetched as a JSON object from an external, unauthenticated API (e.g. the
/// selectielijst of the VNG). Whatever goes wrong (url not allowed, not reachable, not JSON, ...) results in an empty object, see
/// <see cref="IExternalJsonClient"/>. The shape of the external document is unknown to us, so it is stored as is in <c>_expand</c>.
/// </summary>
/// <typeparam name="TParent">The (v1.4) response that is expanded.</typeparam>
public abstract class ExternalJson_ResolverBase<TParent>(IExternalJsonClient externalJsonClient) : IExpandResolver<TParent>
{
    public abstract string Path { get; }
    public string Parent => null;

    /// <summary>The url in the parent that is to be fetched, or null.</summary>
    protected abstract string GetUrl(TParent parent);

    public async Task<object> ResolveAsync(TParent entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        return await externalJsonClient.GetJsonObjectAsync(GetUrl(entity));
    }
}
