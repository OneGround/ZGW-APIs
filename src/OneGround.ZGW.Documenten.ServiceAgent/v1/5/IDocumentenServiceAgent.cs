using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._5;

public interface IDocumentenServiceAgent
{
    Task<
        ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject, object expandedEnkelvoudigInformatieObject)>
    > GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, string expand);
    Task<ServiceAgentResponse<IEnumerable<Contracts.v1.Responses.ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectenAsync(
        GetAllObjectInformatieObjectenQueryParameters parameters
    );
}
