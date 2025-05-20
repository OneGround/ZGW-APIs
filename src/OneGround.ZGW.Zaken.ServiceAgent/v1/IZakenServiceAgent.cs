using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;

namespace OneGround.ZGW.Zaken.ServiceAgent.v1;

public interface IZakenServiceAgent
{
    // Zaak
    Task<ServiceAgentResponse<ZaakResponseDto>> AddZaakAsync(ZaakRequestDto zaak);
    Task<ServiceAgentResponse<ZaakResponseDto>> GetZaakByUrlAsync(string zaakUrl);
    Task<ServiceAgentResponse<ZaakResponseDto>> PatchZaakByUrlAsync(string zaakUrl, JObject zaakPatchRequest);
    Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string zaakUrl);

    // ZaakInformatieObject
    Task<ServiceAgentResponse<ZaakInformatieObjectResponseDto>> AddZaakInformatieObjectAsync(ZaakInformatieObjectRequestDto zaakInformatieObject);
    Task<ServiceAgentResponse<IEnumerable<ZaakInformatieObjectResponseDto>>> GetZaakInformatieObjectenAsync(
        GetAllZaakInformatieObjectenQueryParameters parameters
    );

    // ZaakBesluit
    Task<ServiceAgentResponse<ZaakBesluitResponseDto>> AddZaakBesluitByUrlAsync(Guid zaakId, string besluitUrl);
    Task<ServiceAgentResponse> DeleteZaakBesluitByUrlAsync(string zaakBesluitUrl);

    // ZaakObject
    Task<ServiceAgentResponse<IDictionary<string, object>>> GetZaakObjectByUrlAsync(string zaakObjectUrl);
    Task<ServiceAgentResponse<PagedResponse<JObjectZaakObjectResponseDto>>> GetZaakObjectenAsync(
        GetAllZaakObjectenQueryParameters queryParameters,
        int page = 1
    );

    // ZaakStatus
    Task<ServiceAgentResponse<ZaakStatusResponseDto>> GetZaakStatusByUrlAsync(string zaakStatusUrl);

    // ZaakResultaat
    Task<ServiceAgentResponse<ZaakResultaatResponseDto>> GetZaakResultaatByUrlAsync(string zaakResultaatUrl);

    // ZaakRol
    Task<ServiceAgentResponse<PagedResponse<JObjectZaakRolResponseDto>>> GetZaakRollenAsync(
        GetAllZaakRollenQueryParameters queryParameters,
        int page = 1
    );
    Task<ServiceAgentResponse<ZaakRolResponseDto>> AddZaakRolAsync(ZaakRolRequestDto request);
}
