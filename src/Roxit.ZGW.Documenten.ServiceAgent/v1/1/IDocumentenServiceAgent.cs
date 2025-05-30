using System;
using System.Net.Http;
using System.Threading.Tasks;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Documenten.Contracts.v1._1.Requests;
using Roxit.ZGW.Documenten.Contracts.v1._1.Responses;

namespace Roxit.ZGW.Documenten.ServiceAgent.v1._1;

public interface IDocumentenServiceAgent
{
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectCreateResponseDto>> AddEnkelvoudigInformatieObjectAsync(
        EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObject
    );
    Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectAsync(Guid enkelvoudigInformatieObjectId);
    Task<ServiceAgentResponse<BestandsDeelResponseDto>> AddBestandsdeelAsync(
        string bestandsdeelUrl,
        MultipartFormDataContent multipartFormDataContent
    );
    Task<ServiceAgentResponse> UnlockAsync(string enkelvoudigInformatieObjectUrl);
}
