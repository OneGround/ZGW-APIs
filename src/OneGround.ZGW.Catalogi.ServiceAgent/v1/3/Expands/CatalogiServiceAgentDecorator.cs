using System;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;

/// <summary>
/// This class intercepts calls to the Catalogi service agent and wraps exceptions in an <see cref="ExpandExternalServiceException"/>.
/// <para>
/// Expands must be evaluated by ZTC as the calling client. The first call of a request therefore goes through the
/// <see cref="IUserAuthCatalogiServiceAgent"/> (the caller's own token, no response cache). ZTC authorizes every read the same way (the scope
/// <c>catalogi.lezen</c> and the RSIN, nothing per object), so once ZTC has accepted the caller, the remaining calls of that request are made
/// through the <see cref="ICatalogiServiceAgent"/> (v1.3). The HTTP pipeline of that agent has the <c>CachingHandler</c>: the distributed (Redis)
/// response cache, keyed by RSIN, url and API version, which keeps an entry for a few hours. It is the very same cache, with the same entries,
/// that the v1.5 expands use. That way a request costs at most one uncached ZTC call, without a caller who may not read ZTC ever being served
/// from that cache. A call that fails does not count as accepted, so the next call tries the caller's agent again.
/// </para>
/// <para>
/// Two cache layers, with different jobs: in front of this class the resolvers keep a cache per request (<c>IGenericCache</c>, in memory), so
/// the same url reaches this class at most once per request (100 zaken with the same zaaktype are one call); the Redis cache behind it is the
/// layer between requests.
/// </para>
/// <para>
/// Registered scoped: the "accepted" state lives for one request (one caller).
/// </para>
/// </summary>
public sealed class CatalogiServiceAgentDecorator : ICatalogiServiceAgentDecorator
{
    private const string ServiceName = "ZTC";
    private readonly IUserAuthCatalogiServiceAgent _callerAgent;
    private readonly ICatalogiServiceAgent _cachedAgent;
    private int _callerIsAccepted;

    public CatalogiServiceAgentDecorator(IUserAuthCatalogiServiceAgent callerAgent, ICatalogiServiceAgent cachedAgent)
    {
        _callerAgent = callerAgent;
        _cachedAgent = cachedAgent;
    }

    public Task<ServiceAgentResponse<CatalogusResponseDto>> AddCatalogusAsync(CatalogusRequestDto request) => throw new NotImplementedException();

    public Task<ServiceAgentResponse<BesluitTypeResponseDto>> GetBesluitTypeByUrlAsync(string besluitTypeUrl, string expand = null) =>
        WrapAsync(besluitTypeUrl, agent => agent.GetBesluitTypeByUrlAsync(besluitTypeUrl, expand));

    public Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(string catalogusUrl) =>
        WrapAsync(catalogusUrl, agent => agent.GetCatalogusAsync(catalogusUrl));

    public Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(Guid catalogusId)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<PagedResponse<CatalogusResponseDto>>> GetCatalogussenAsync(
        Contracts.v1.Queries.GetAllCatalogussenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<EigenschapResponseDto>> GetEigenschapByUrlAsync(string eigenschapUrl) =>
        WrapAsync(eigenschapUrl, agent => agent.GetEigenschapByUrlAsync(eigenschapUrl));

    public Task<ServiceAgentResponse<InformatieObjectTypeResponseDto>> GetInformatieObjectTypeByUrlAsync(
        string informatieObjectTypeUrl,
        string expand = null
    ) => WrapAsync(informatieObjectTypeUrl, agent => agent.GetInformatieObjectTypeByUrlAsync(informatieObjectTypeUrl, expand));

    public Task<ServiceAgentResponse<PagedResponse<InformatieObjectTypeResponseDto>>> GetInformatieObjectTypenAsync(
        Contracts.v1._2.Queries.GetAllInformatieObjectTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<ResultaatTypeResponseDto>> GetResultaatTypeByUrlAsync(string resultaatTypeUrl) =>
        WrapAsync(resultaatTypeUrl, agent => agent.GetResultaatTypeByUrlAsync(resultaatTypeUrl));

    public Task<ServiceAgentResponse<RolTypeResponseDto>> GetRolTypeByUrlAsync(string rolTypeUrl) =>
        WrapAsync(rolTypeUrl, agent => agent.GetRolTypeByUrlAsync(rolTypeUrl));

    public Task<ServiceAgentResponse<PagedResponse<RolTypeResponseDto>>> GetRolTypenAsync(
        Contracts.v1._3.Queries.GetAllRolTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<StatusTypeResponseDto>> GetStatusTypeByUrlAsync(string statusTypeUrl) =>
        WrapAsync(statusTypeUrl, agent => agent.GetStatusTypeByUrlAsync(statusTypeUrl));

    public Task<ServiceAgentResponse<ZaakObjectTypeResponseDto>> GetZaakObjectTypeByUrlAsync(string zaakObjectTypeUrl) =>
        WrapAsync(zaakObjectTypeUrl, agent => agent.GetZaakObjectTypeByUrlAsync(zaakObjectTypeUrl));

    public Task<ServiceAgentResponse<ZaakTypeResponseDto>> GetZaakTypeByUrlAsync(string zaakTypeUrl, string expand = null) =>
        WrapAsync(zaakTypeUrl, agent => agent.GetZaakTypeByUrlAsync(zaakTypeUrl, expand));

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>>> GetZaakTypeInformatieObjectTypenAsync(
        Contracts.v1._3.Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeResponseDto>>> GetZaakTypenAsync(
        Contracts.v1.Queries.GetAllZaakTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    private async Task<ServiceAgentResponse<T>> WrapAsync<T>(string url, Func<ICatalogiServiceAgent, Task<ServiceAgentResponse<T>>> call)
    {
        try
        {
            var callerIsAccepted = Volatile.Read(ref _callerIsAccepted) == 1;

            var result = await call(callerIsAccepted ? _cachedAgent : _callerAgent);
            if (!result.Success || result.Response == null)
            {
                throw ExpandExternalServiceException.ForFailedResponse(ServiceName, url, result);
            }

            if (!callerIsAccepted)
            {
                Volatile.Write(ref _callerIsAccepted, 1);
            }

            return result;
        }
        catch (Exception ex) when (ex is not ExpandExternalServiceException)
        {
            throw new ExpandExternalServiceException(ServiceName, url, ex);
        }
    }
}
