using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.ServiceAgent.v1;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "besluit" expand path on a ZAAKBESLUIT from BRC, through the user-authenticated <see cref="IUserAuthBesluitenServiceAgent"/> so
/// BRC evaluates the request as the real calling client. A failed call throws <see cref="ExpandExternalServiceException"/>.
/// </summary>
public class ZaakBesluitBesluitResolver : IExpandResolver<ZaakBesluitResponseDto>
{
    private const string ServiceName = ServiceRoleName.BRC;
    private readonly IUserAuthBesluitenServiceAgent _besluitenServiceAgent;
    private readonly IGenericCache<ServiceAgentResponse<BesluitResponseDto>> _besluitCache;

    public ZaakBesluitBesluitResolver(
        IUserAuthBesluitenServiceAgent besluitenServiceAgent,
        IGenericCache<ServiceAgentResponse<BesluitResponseDto>> besluitCache
    )
    {
        _besluitenServiceAgent = besluitenServiceAgent;
        _besluitCache = besluitCache;
    }

    public string Path => "besluit";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakBesluitResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        // Note: once per request and url (the user-authenticated agent has no cache of its own). Only an exception is not kept; a response that is
        // not a success is, so every caller must handle it as below (403/404 => nothing to expand, anything else => the request fails).
        var result = await _besluitCache.GetOrCacheAndGetAsync(
            $"besluit_{entity.Besluit}",
            () => _besluitenServiceAgent.GetBesluitByUrlAsync(entity.Besluit)
        );

        if (!result.Success || result.Response == null)
        {
            // Note: BRC evaluates the request as the caller. A besluit that BRC says is not available to the caller (403/404) is left out of the
            // expand, like an entity that the same-service query does not return (see ExpandQueryStatus), instead of failing the request.
            if (ExpandExternalServiceException.IsNotAvailableToCaller(result))
            {
                return null;
            }

            throw ExpandExternalServiceException.ForFailedResponse(ServiceName, entity.Besluit, result);
        }

        return result.Response;
    }
}
