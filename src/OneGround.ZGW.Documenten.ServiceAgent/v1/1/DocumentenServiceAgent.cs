using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._1.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._1;

public class DocumentenServiceAgent : ZGWServiceAgent<DocumentenServiceAgent>, IDocumentenServiceAgent
{
    public DocumentenServiceAgent(
        ILogger<DocumentenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.DRC, "v1")
    {
        Client.DefaultRequestHeaders.Add("Api-Version", "1.1");
    }

    public Task<ServiceAgentResponse<EnkelvoudigInformatieObjectCreateResponseDto>> AddEnkelvoudigInformatieObjectAsync(
        EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObject
    )
    {
        var url = new Uri("/enkelvoudiginformatieobjecten", UriKind.Relative);

        return PostAsync<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectCreateResponseDto>(url, enkelvoudigInformatieObject);
    }

    public Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectAsync(Guid enkelvoudigInformatieObjectId)
    {
        if (enkelvoudigInformatieObjectId == Guid.Empty)
            throw new ArgumentNullException(nameof(enkelvoudigInformatieObjectId));

        Logger.LogDebug("Getting document by id: {enkelvoudigInformatieObjectId}", enkelvoudigInformatieObjectId);

        var url = new Uri($"/enkelvoudiginformatieobjecten/{enkelvoudigInformatieObjectId}", UriKind.Relative);

        return GetAsync<EnkelvoudigInformatieObjectResponseDto>(url);
    }

    public async Task<ServiceAgentResponse<BestandsDeelResponseDto>> AddBestandsdeelAsync(
        string bestandsdeelUrl,
        MultipartFormDataContent multipartFormDataContent
    )
    {
        return await PutAsync<BestandsDeelResponseDto>(new Uri(bestandsdeelUrl), multipartFormDataContent);
    }

    public async Task<ServiceAgentResponse> UnlockAsync(string enkelvoudigInformatieObjectUrl)
    {
        var unlockEnkelvoudigInformatieObjectUrl = enkelvoudigInformatieObjectUrl + "/unlock";

        return await PostAsync(new Uri(unlockEnkelvoudigInformatieObjectUrl));
    }
}
