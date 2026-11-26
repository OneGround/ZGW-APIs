using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._7;

public interface IDocumentenServiceAgent
{
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectCreateResponseDto>> AddEnkelvoudigInformatieObjectAsync(
        EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObject
    );
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectByUrlAsync(
        string enkelvoudigInformatieObjectUrl
    );
    Task<
        ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject, object expandedEnkelvoudigInformatieObject)>
    > GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, string expand);
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectAsync(Guid enkelvoudigInformatieObjectId);
    Task<ServiceAgentResponse<Contracts.v1._1.Responses.BestandsDeelResponseDto>> AddBestandsdeelAsync(
        string bestandsdeelUrl,
        MultipartFormDataContent multipartFormDataContent
    );
    Task<ServiceAgentResponse> UnlockAsync(string enkelvoudigInformatieObjectUrl);
    Task<ServiceAgentResponse> DeleteEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl);
    Task<ServiceAgentResponse<Stream>> DownloadEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, int? version = null);
    Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(
        string informatieObject,
        string @object
    );
    Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectenAsync(
        GetAllObjectInformatieObjectenQueryParameters parameters
    );

    // Note: POST objectinformatieobjecten only exists in the v1.0 controller (there is no 1.7 variant), so request and response are the v1.0 DTOs,
    // unlike the GET calls above which return the 1.7 response DTO.
    Task<ServiceAgentResponse<Contracts.v1.Responses.ObjectInformatieObjectResponseDto>> AddObjectInformatieObjectAsync(
        Contracts.v1.Requests.ObjectInformatieObjectRequestDto objectInformatieObject
    );
    Task<ServiceAgentResponse> DeleteObjectInformatieObjectByUrlAsync(string objectInformatieObjectUrl);
    Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string enkelvoudigInformatieObjectUrl);
}
