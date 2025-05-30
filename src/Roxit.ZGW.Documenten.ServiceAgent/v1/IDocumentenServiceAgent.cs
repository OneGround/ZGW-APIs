using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Contracts.v1.AuditTrail;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;

namespace Roxit.ZGW.Documenten.ServiceAgent.v1;

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
