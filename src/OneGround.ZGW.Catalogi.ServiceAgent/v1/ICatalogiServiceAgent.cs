using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1;

public interface ICatalogiServiceAgent
{
    // Catalogus
    Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(string catalogusUrl);

    // BesluitType
    Task<ServiceAgentResponse<BesluitTypeResponseDto>> GetBesluitTypeByUrlAsync(string besluitTypeUrl);

    // ZaakType
    Task<ServiceAgentResponse<ZaakTypeResponseDto>> GetZaakTypeByUrlAsync(string zaakTypeUrl);
    Task<ServiceAgentResponse<PagedResponse<ZaakTypeResponseDto>>> GetZaakTypenAsync(GetAllZaakTypenQueryParameters parameters, int page = 1);

    // StatusType
    Task<ServiceAgentResponse<StatusTypeResponseDto>> GetStatusTypeByUrlAsync(string statusTypeUrl);

    // ResultaatType
    Task<ServiceAgentResponse<ResultaatTypeResponseDto>> GetResultaatTypeByUrlAsync(string resultaatTypeUrl);

    // RolType
    Task<ServiceAgentResponse<RolTypeResponseDto>> GetRolTypeByUrlAsync(string rolTypeUrl);
    Task<ServiceAgentResponse<PagedResponse<RolTypeResponseDto>>> GetRolTypenAsync(GetAllRolTypenQueryParameters parameters, int page = 1);

    // InformatieObjectType
    Task<ServiceAgentResponse<InformatieObjectTypeResponseDto>> GetInformatieObjectTypeByUrlAsync(string informatieObjectTypeUrl);

    // Eigenschap
    Task<ServiceAgentResponse<EigenschapResponseDto>> GetEigenschapByUrlAsync(string eigenschapUrl);

    // ZaakType-InformatieObjectType
    Task<ServiceAgentResponse<PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>>> GetZaakTypeInformatieObjectTypenAsync(
        GetAllZaakTypeInformatieObjectTypenQueryParameters parameters,
        int page = 1
    );
}
