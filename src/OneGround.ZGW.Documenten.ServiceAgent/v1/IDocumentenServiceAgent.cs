using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1;

public interface IDocumentenServiceAgent
{
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectCreateResponseDto>> AddEnkelvoudigInformatieObjectAsync(
        EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObject
    );
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectByUrlAsync(
        string enkelvoudigInformatieObjectUrl
    );
    Task<ServiceAgentResponse> DeleteEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl);
    Task<ServiceAgentResponse<Stream>> DownloadEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, int? version = null);
    Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(
        string informatieObject,
        string @object
    );
    Task<ServiceAgentResponse<ObjectInformatieObjectResponseDto>> AddObjectInformatieObjectAsync(
        ObjectInformatieObjectRequestDto objectInformatieObject
    );
    Task<ServiceAgentResponse> DeleteObjectInformatieObjectByUrlAsync(string objectInformatieObjectUrl);
    Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string enkelvoudigInformatieObjectUrl);
}
