using System;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1._4;

public interface ICatalogiServiceAgent
{
    // Catalogus
    Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(string catalogusUrl);
    Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(Guid catalogusId);
    Task<ServiceAgentResponse<PagedResponse<CatalogusResponseDto>>> GetCatalogussenAsync(GetAllCatalogussenQueryParameters parameters, int page = 1);
    Task<ServiceAgentResponse<CatalogusResponseDto>> AddCatalogusAsync(Contracts.v1._3.Requests.CatalogusRequestDto request);

    // BesluitType
    Task<ServiceAgentResponse<BesluitTypeResponseDto>> GetBesluitTypeByUrlAsync(string besluitTypeUrl, string expand = null);

    // ZaakType
    Task<ServiceAgentResponse<ZaakTypeResponseDto>> GetZaakTypeByUrlAsync(string zaakTypeUrl, string expand = null);
    Task<ServiceAgentResponse<PagedResponse<ZaakTypeResponseDto>>> GetZaakTypenAsync(GetAllZaakTypenQueryParameters parameters, int page = 1);

    // StatusType
    Task<ServiceAgentResponse<Contracts.v1._3.Responses.StatusTypeResponseDto>> GetStatusTypeByUrlAsync(string statusTypeUrl);

    // ResultaatType
    Task<ServiceAgentResponse<Contracts.v1._3.Responses.ResultaatTypeResponseDto>> GetResultaatTypeByUrlAsync(string resultaatTypeUrl);

    // RolType
    Task<ServiceAgentResponse<Contracts.v1._3.Responses.RolTypeResponseDto>> GetRolTypeByUrlAsync(string rolTypeUrl);
    Task<ServiceAgentResponse<PagedResponse<Contracts.v1._3.Responses.RolTypeResponseDto>>> GetRolTypenAsync(
        Contracts.v1._3.Queries.GetAllRolTypenQueryParameters parameters,
        int page = 1
    );

    // InformatieObjectType
    Task<ServiceAgentResponse<InformatieObjectTypeResponseDto>> GetInformatieObjectTypeByUrlAsync(
        string informatieObjectTypeUrl,
        string expand = null
    );
    Task<ServiceAgentResponse<PagedResponse<InformatieObjectTypeResponseDto>>> GetInformatieObjectTypenAsync(
        GetAllInformatieObjectTypenQueryParameters parameters,
        int page = 1
    );

    // Eigenschap
    Task<ServiceAgentResponse<Contracts.v1._3.Responses.EigenschapResponseDto>> GetEigenschapByUrlAsync(string eigenschapUrl);

    // ZaakType-InformatieObjectType
    Task<
        ServiceAgentResponse<PagedResponse<Contracts.v1._3.Responses.ZaakTypeInformatieObjectTypeResponseDto>>
    > GetZaakTypeInformatieObjectTypenAsync(Contracts.v1._3.Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters parameters, int page = 1);

    // ZaakObjectTypeDto
    Task<ServiceAgentResponse<Contracts.v1._3.Responses.ZaakObjectTypeResponseDto>> GetZaakObjectTypeByUrlAsync(string zaakObjectTypeUrl);
}
