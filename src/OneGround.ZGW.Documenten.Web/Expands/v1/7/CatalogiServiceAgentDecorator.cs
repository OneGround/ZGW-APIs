using System;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

// TODO: Move to CatalogiServiceAgent class (later BRC/ZRC should reuse this class as well)
public sealed class CatalogiServiceAgentDecorator : ICatalogiServiceAgentDecorator
{
    private const string ServiceName = "ZTC";
    private readonly ICatalogiServiceAgent _inner;

    public CatalogiServiceAgentDecorator(ICatalogiServiceAgent inner) => _inner = inner;

    public Task<ServiceAgentResponse<CatalogusResponseDto>> AddCatalogusAsync(CatalogusRequestDto request) => throw new NotImplementedException();

    public Task<ServiceAgentResponse<BesluitTypeResponseDto>> GetBesluitTypeByUrlAsync(string besluitTypeUrl)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(string catalogusUrl)
    {
        try
        {
            var result = await _inner.GetCatalogusAsync(catalogusUrl);
            if (!result.Success || result.Response == null)
            {
                throw new ExpandExternalServiceException(ServiceName, catalogusUrl, null);
            }
            return result;
        }
        catch (Exception ex) when (ex is not ExpandExternalServiceException)
        {
            throw new ExpandExternalServiceException(ServiceName, catalogusUrl, ex);
        }
    }

    public Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(Guid catalogusId)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<PagedResponse<CatalogusResponseDto>>> GetCatalogussenAsync(
        Catalogi.Contracts.v1.Queries.GetAllCatalogussenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<EigenschapResponseDto>> GetEigenschapByUrlAsync(string eigenschapUrl)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceAgentResponse<InformatieObjectTypeResponseDto>> GetInformatieObjectTypeByUrlAsync(string informatieObjectTypeUrl)
    {
        try
        {
            var result = await _inner.GetInformatieObjectTypeByUrlAsync(informatieObjectTypeUrl);
            if (!result.Success || result.Response == null)
            {
                throw new ExpandExternalServiceException(ServiceName, informatieObjectTypeUrl, null);
            }
            return result;
        }
        catch (Exception ex) when (ex is not ExpandExternalServiceException)
        {
            throw new ExpandExternalServiceException(ServiceName, informatieObjectTypeUrl, ex);
        }
    }

    public Task<ServiceAgentResponse<PagedResponse<InformatieObjectTypeResponseDto>>> GetInformatieObjectTypenAsync(
        Catalogi.Contracts.v1._2.Queries.GetAllInformatieObjectTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<ResultaatTypeResponseDto>> GetResultaatTypeByUrlAsync(string resultaatTypeUrl)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<RolTypeResponseDto>> GetRolTypeByUrlAsync(string rolTypeUrl)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<PagedResponse<RolTypeResponseDto>>> GetRolTypenAsync(
        Catalogi.Contracts.v1._3.Queries.GetAllRolTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<StatusTypeResponseDto>> GetStatusTypeByUrlAsync(string statusTypeUrl)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<ZaakObjectTypeResponseDto>> GetZaakObjectTypeByUrlAsync(string zaakObjectTypeUrl)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<ZaakTypeResponseDto>> GetZaakTypeByUrlAsync(string zaakTypeUrl)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>>> GetZaakTypeInformatieObjectTypenAsync(
        Catalogi.Contracts.v1._3.Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeResponseDto>>> GetZaakTypenAsync(
        Catalogi.Contracts.v1.Queries.GetAllZaakTypenQueryParameters parameters,
        int page = 1
    )
    {
        throw new NotImplementedException();
    }
}
