using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Base for the top-level ZAAK expand paths whose value is an optional url that is fetched as a JSON object from an external,
/// unauthenticated API. Whatever goes wrong (url not allowed, not reachable, not JSON, ...) results in an empty object, see
/// <see cref="IExternalJsonClient"/>. There is deliberately no "fields" schema entry for these paths -- the shape of the external
/// document is unknown to us.
/// </summary>
public abstract class ZaakExternalJsonResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IExternalJsonClient _externalJsonClient;

    protected ZaakExternalJsonResolver(IExternalJsonClient externalJsonClient)
    {
        _externalJsonClient = externalJsonClient;
    }

    public abstract string Path { get; }
    public string Parent => null;

    protected abstract string GetUrl(ZaakResponseDto zaak);

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        return await _externalJsonClient.GetJsonObjectAsync(GetUrl(entity));
    }
}
