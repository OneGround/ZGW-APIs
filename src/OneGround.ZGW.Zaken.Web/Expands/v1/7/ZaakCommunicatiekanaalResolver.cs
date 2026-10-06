using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "communicatiekanaal" expand path: the optional url on a ZAAK is fetched as a JSON object from an external,
/// unauthenticated API. Whatever goes wrong (url not allowed, not reachable, not JSON, ...) results in an empty object, see
/// <see cref="IExternalJsonClient"/>. There is deliberately no "fields" schema entry for this path -- the shape of the external
/// document is unknown to us.
/// </summary>
public class ZaakCommunicatiekanaalResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IExternalJsonClient _externalJsonClient;

    public ZaakCommunicatiekanaalResolver(IExternalJsonClient externalJsonClient)
    {
        _externalJsonClient = externalJsonClient;
    }

    public string Path => "communicatiekanaal";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        return await _externalJsonClient.GetJsonObjectAsync(entity.Communicatiekanaal);
    }
}
