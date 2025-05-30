using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Common.Services;
using Roxit.ZGW.Documenten.Contracts.v1._5.Responses;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;

namespace Roxit.ZGW.Documenten.ServiceAgent.v1._5;

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
        Client.DefaultRequestHeaders.Add("Api-Version", "1.5");
    }

    public async Task<
        ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject, object expandedEnkelvoudigInformatieObject)>
    > GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, string expand)
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, enkelvoudigInformatieObjectUrl, "enkelvoudiginformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse<(
                EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject,
                object expandedEnkelvoudigInformatieObject
            )>(errorResponse);

        Logger.LogDebug("EnkelvoudigInformatieObject bevragen op '{enkelvoudigInformatieObjectUrl}'....", enkelvoudigInformatieObjectUrl);

        var url = new Uri(enkelvoudigInformatieObjectUrl);

        if (!string.IsNullOrEmpty(expand))
        {
            var result = await GetAsync<object>(url.AddQueryParameter("expand", expand));
            if (!result.Success)
            {
                return new ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto, object)>(result.Error, null);
            }
            var enkelvoudiginformatieobjectBase = JsonConvert.DeserializeObject<EnkelvoudigInformatieObjectResponseDto>(result.Response.ToString());

            return new ServiceAgentResponse<(
                EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject,
                object expandedEnkelvoudigInformatieObject
            )>((enkelvoudiginformatieobjectBase, result.Response));
        }
        else
        {
            var result = await GetAsync<EnkelvoudigInformatieObjectResponseDto>(url);
            if (!result.Success)
            {
                return new ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto, object)>(result.Error, null);
            }
            return new ServiceAgentResponse<(
                EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject,
                object expandedEnkelvoudigInformatieObject
            )>((result.Response, result.Response));
        }
    }

    public async Task<ServiceAgentResponse<IEnumerable<Contracts.v1.Responses.ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectenAsync(
        GetAllObjectInformatieObjectenQueryParameters parameters
    )
    {
        return await GetAsync<Contracts.v1.Responses.ObjectInformatieObjectResponseDto>("/objectinformatieobjecten", parameters);
    }
}
