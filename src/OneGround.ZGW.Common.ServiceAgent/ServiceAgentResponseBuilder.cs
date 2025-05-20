using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts.v1;

namespace OneGround.ZGW.Common.ServiceAgent;

public interface IServiceAgentResponseBuilder
{
    Task<ServiceAgentResponse<TResponse>> CreateAsync<TResponse>(HttpResponseMessage httpResponseMessage);
    Task<ServiceAgentResponse> CreateAsync(HttpResponseMessage httpResponseMessage);
}

public class ServiceAgentResponseBuilder : IServiceAgentResponseBuilder
{
    private readonly JsonSerializerSettings _jsonSettings = new ZGWJsonSerializerSettings();

    public async Task<ServiceAgentResponse<TResponse>> CreateAsync<TResponse>(HttpResponseMessage httpResponseMessage)
    {
        var content = await httpResponseMessage.Content.ReadAsStringAsync();

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            var response = JsonConvert.DeserializeObject<TResponse>(content, _jsonSettings);
            return new ServiceAgentResponse<TResponse>(response);
        }

        if (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
        {
            var badRequestResponse = JsonConvert.DeserializeObject<ErrorResponse>(content, _jsonSettings);
            if (badRequestResponse != null)
            {
                return new ServiceAgentResponse<TResponse>(badRequestResponse);
            }
        }

        var errorResponse = CreateErrorResponse(httpResponseMessage);
        return new ServiceAgentResponse<TResponse>(errorResponse);
    }

    public async Task<ServiceAgentResponse> CreateAsync(HttpResponseMessage httpResponseMessage)
    {
        string content = await httpResponseMessage.Content.ReadAsStringAsync();

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            return new ServiceAgentResponse();
        }

        if (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
        {
            var badRequestResponse = JsonConvert.DeserializeObject<ErrorResponse>(content, _jsonSettings);
            if (badRequestResponse != null)
            {
                return new ServiceAgentResponse(badRequestResponse);
            }
        }

        var errorResponse = CreateErrorResponse(httpResponseMessage);
        return new ServiceAgentResponse(errorResponse);
    }

    private static ErrorResponse CreateErrorResponse(HttpResponseMessage httpResponseMessage)
    {
        return new ErrorResponse
        {
            Status = (int)httpResponseMessage.StatusCode,
            Code = ErrorCode.BadUrl,
            Title =
                $"De URL {httpResponseMessage.RequestMessage.RequestUri} gaf als antwoord HTTP {(int)httpResponseMessage.StatusCode}. Geef een geldige URL op.",
        };
    }
}
