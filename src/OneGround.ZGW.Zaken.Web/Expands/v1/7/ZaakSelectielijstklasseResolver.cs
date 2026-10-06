using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "selectielijstklasse" expand path: the optional url on a ZAAK is fetched as a JSON object from an external,
/// unauthenticated API. Same behaviour (and same reasons for no "fields" support) as <see cref="ZaakCommunicatiekanaalResolver"/>.
/// </summary>
public class ZaakSelectielijstklasseResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IExternalJsonClient _externalJsonClient;

    public ZaakSelectielijstklasseResolver(IExternalJsonClient externalJsonClient)
    {
        _externalJsonClient = externalJsonClient;
    }

    public string Path => "selectielijstklasse";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        return await _externalJsonClient.GetJsonObjectAsync(entity.Selectielijstklasse);
    }
}
