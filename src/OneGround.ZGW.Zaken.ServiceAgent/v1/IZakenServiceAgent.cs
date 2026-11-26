using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using V10 = OneGround.ZGW.Zaken.Contracts.v1;
using V15 = OneGround.ZGW.Zaken.Contracts.v1._5;

namespace OneGround.ZGW.Zaken.ServiceAgent.v1;

public interface IZakenServiceAgent
{
    // Zaak
    Task<ServiceAgentResponse<ZaakResponseDto>> AddZaakAsync(V15.Requests.ZaakRequestDto zaak);
    Task<ServiceAgentResponse<ZaakResponseDto>> GetZaakByUrlAsync(string zaakUrl);
    Task<ServiceAgentResponse<ZaakResponseDto>> PatchZaakByUrlAsync(string zaakUrl, JObject zaakPatchRequest);
    Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string zaakUrl);

    // ZaakInformatieObject
    Task<ServiceAgentResponse<V15.Responses.ZaakInformatieObjectResponseDto>> AddZaakInformatieObjectAsync(
        V15.Requests.ZaakInformatieObjectRequestDto zaakInformatieObject
    );
    Task<ServiceAgentResponse<IEnumerable<ZaakInformatieObjectResponseDto>>> GetZaakInformatieObjectenAsync(
        GetAllZaakInformatieObjectenQueryParameters parameters
    );
    Task<ServiceAgentResponse> DeleteZaakInformatieObjectByIdAsync(Guid zaakInformatieObjectId);

    // ZaakBesluit
    Task<ServiceAgentResponse<V10.Responses.ZaakBesluitResponseDto>> AddZaakBesluitByUrlAsync(Guid zaakId, string besluitUrl);
    Task<ServiceAgentResponse> DeleteZaakBesluitByUrlAsync(string zaakBesluitUrl);

    // ZaakObject
    Task<ServiceAgentResponse<IDictionary<string, object>>> GetZaakObjectByUrlAsync(string zaakObjectUrl);
    Task<ServiceAgentResponse<PagedResponse<JObjectZaakObjectResponseDto>>> GetZaakObjectenAsync(
        GetAllZaakObjectenQueryParameters queryParameters,
        int page = 1
    );

    // ZaakStatus
    Task<ServiceAgentResponse<StatusResponseDto>> GetZaakStatusByUrlAsync(string zaakStatusUrl);

    // ZaakResultaat
    Task<ServiceAgentResponse<ResultaatResponseDto>> GetZaakResultaatByUrlAsync(string zaakResultaatUrl);

    // ZaakRol
    Task<ServiceAgentResponse<PagedResponse<JObjectZaakRolResponseDto>>> GetZaakRollenAsync(
        GetAllZaakRollenQueryParameters queryParameters,
        int page = 1
    );
    Task<ServiceAgentResponse<V15.Responses.ZaakRol.ZaakRolResponseDto>> AddZaakRolAsync(V15.Requests.ZaakRol.ZaakRolRequestDto request);
}
