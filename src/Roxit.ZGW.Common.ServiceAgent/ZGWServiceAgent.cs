using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Contracts.Extensions;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Services;

namespace Roxit.ZGW.Common.ServiceAgent;

public abstract class ZGWServiceAgent<T>
    where T : class
{
    private readonly IServiceAgentResponseBuilder _responseBuilder;
    private readonly JsonSerializerSettings _jsonSettings = new ZGWJsonSerializerSettings();
    protected readonly HttpClient Client;
    protected readonly ILogger<T> Logger;

    protected ZGWServiceAgent(
        HttpClient client,
        ILogger<T> logger,
        IServiceDiscovery serviceDiscovery,
        IConfiguration configuration,
        IServiceAgentResponseBuilder responseBuilder,
        string serviceRoleName,
        string version = "v1"
    )
    {
        Client = client;
        Logger = logger;
        _responseBuilder = responseBuilder;

        client.BaseAddress = serviceDiscovery.GetApi(serviceRoleName, version);
        client.Timeout = configuration.GetValue($"ServiceAgent:{serviceRoleName}:Timeout", client.Timeout);
    }

    protected Task<Stream> GetStreamAsync(Uri uri)
    {
        return Client.GetStreamAsync(uri);
    }

    protected async Task<ServiceAgentResponse<TResponse>> GetAsync<TResponse>(Uri url, params (string, string)[] headers)
    {
        var uri = GetAbsoluteUri(url);

        return await SendRequestAsync<TResponse>(HttpMethod.Get, uri, headers);
    }

    protected Task<ServiceAgentResponse<TResponse>> PostAsync<TRequest, TResponse>(Uri url, TRequest request, params (string, string)[] headers)
    {
        var uri = GetAbsoluteUri(url);

        return SendRequestAsync<TRequest, TResponse>(HttpMethod.Post, uri, request, headers);
    }

    protected Task<ServiceAgentResponse<TResponse>> PutAsync<TRequest, TResponse>(Uri url, TRequest request, params (string, string)[] headers)
    {
        var uri = GetAbsoluteUri(url);

        return SendRequestAsync<TRequest, TResponse>(HttpMethod.Put, uri, request, headers);
    }

    protected async Task<ServiceAgentResponse<TResponse>> PutAsync<TResponse>(Uri url, MultipartFormDataContent content)
    {
        var uri = GetAbsoluteUri(url);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, uri);
        httpRequest.Content = content;

        return await SendAsync<TResponse>(httpRequest);
    }

    public Task<ServiceAgentResponse> PostAsync(Uri url)
    {
        var uri = GetAbsoluteUri(url);

        return SendRequestAsync(HttpMethod.Post, uri);
    }

    public Task<ServiceAgentResponse<TResponse>> PatchAsync<TResponse>(Uri url, JObject request, params (string, string)[] headers)
        where TResponse : class
    {
        var uri = GetAbsoluteUri(url);

        return SendRequestAsync<JObject, TResponse>(HttpMethod.Patch, uri, request, headers);
    }

    public Task<ServiceAgentResponse> DeleteAsync(Uri url)
    {
        var uri = GetAbsoluteUri(url);

        return SendRequestAsync(HttpMethod.Delete, uri);
    }

    private async Task<ServiceAgentResponse<TResponse>> SendRequestAsync<TResponse>(HttpMethod httpMethod, Uri uri, params (string, string)[] headers)
    {
        using var httpRequest = new HttpRequestMessage(httpMethod, uri);

        foreach (var header in headers)
        {
            httpRequest.Headers.Add(header.Item1, header.Item2);
        }

        return await SendAsync<TResponse>(httpRequest);
    }

    private async Task<ServiceAgentResponse<TResponse>> SendRequestAsync<TRequest, TResponse>(
        HttpMethod httpMethod,
        Uri uri,
        TRequest request,
        params (string, string)[] headers
    )
    {
        using var httpRequest = new HttpRequestMessage(httpMethod, uri);
        httpRequest.Content = new StringContent(JsonConvert.SerializeObject(request, _jsonSettings), Encoding.UTF8, "application/json");

        foreach (var header in headers)
        {
            httpRequest.Headers.Add(header.Item1, header.Item2);
        }

        return await SendAsync<TResponse>(httpRequest);
    }

    private async Task<ServiceAgentResponse> SendRequestAsync(HttpMethod httpMethod, Uri uri, params (string, string)[] headers)
    {
        using var httpRequest = new HttpRequestMessage(httpMethod, uri);

        foreach (var header in headers)
        {
            httpRequest.Headers.Add(header.Item1, header.Item2);
        }

        return await SendAsync(httpRequest);
    }

    private async Task<ServiceAgentResponse> SendAsync(HttpRequestMessage httpRequest)
    {
        try
        {
            var response = await Client.SendAsync(httpRequest);
            LogUnsuccessfulResponse(response);
            return await _responseBuilder.CreateAsync(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read http response.");

            var error = new ErrorResponse
            {
                Code = ErrorCode.InvalidResource,
                Title = $"De URL {httpRequest.RequestUri} gaf geen antwoord. Raadpleeg applicatiebeheerder.",
            };

            return new ServiceAgentResponse(error, ex);
        }
    }

    private void LogUnsuccessfulResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError(
                "{Method} request to {RequestUri} failed with status code: {StatusCode}",
                response.RequestMessage.Method,
                response.RequestMessage.RequestUri,
                response.StatusCode
            );
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (response.Headers.WwwAuthenticate.Count > 0)
                {
                    Logger.LogError("Request unauthorized. WWW-Authenticate: {response.Headers.WwwAuthenticate}", response.Headers.WwwAuthenticate);
                }
            }
        }
    }

    private async Task<ServiceAgentResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage httpRequest)
    {
        try
        {
            var response = await Client.SendAsync(httpRequest);
            LogUnsuccessfulResponse(response);
            return await _responseBuilder.CreateAsync<TResponse>(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read http response.");

            var error = new ErrorResponse
            {
                Code = ErrorCode.InvalidResource,
                Title = $"De URL {httpRequest.RequestUri} gaf geen antwoord. Raadpleeg applicatiebeheerder.",
            };

            return new ServiceAgentResponse<TResponse>(error, ex);
        }
    }

    protected Task<ServiceAgentResponse<PagedResponse<TResponse>>> GetPagedResponseAsync<TResponse>(
        string relativeUri,
        IQueryParameters queryParameters,
        int page = 1
    )
    {
        var url = new Uri(relativeUri, UriKind.Relative).AddQueryParameter("page", page);

        // Note: For ZGW API's size query-parameter is not supported (so we want not to see this in log)
        return GetPagedResponseAsync<TResponse>(queryParameters, ref url);
    }

    protected Task<ServiceAgentResponse<PagedResponse<TResponse>>> GetPagedResponseAsync<TResponse>(IQueryParameters queryParameters, ref Uri url)
    {
        url = queryParameters
            .GetParameters()
            .Aggregate(url, (url, parameter) => url.AddQueryParameter(parameter.QueryName, parameter.GetValue(queryParameters)));

        Logger.LogDebug("GetPagedResponseAsync '{url}'", url);

        return GetAsync<PagedResponse<TResponse>>(url);
    }

    protected Task<ServiceAgentResponse<IEnumerable<TResponse>>> GetAsync<TResponse>(string relativeUri, IQueryParameters queryParameters = null)
    {
        var url = new Uri(relativeUri, UriKind.Relative);

        if (queryParameters != null)
        {
            url = queryParameters
                .GetParameters()
                .Aggregate(url, (url, parameter) => url.AddQueryParameter(parameter.QueryName, parameter.GetValue(queryParameters)));
        }

        Logger.LogDebug("GetAsync '{url}'", url);

        return GetAsync<IEnumerable<TResponse>>(url);
    }

    protected Uri GetAbsoluteUri(Uri relativeOrAbsoluteUri)
    {
        ArgumentNullException.ThrowIfNull(relativeOrAbsoluteUri);

        if (relativeOrAbsoluteUri.IsAbsoluteUri)
        {
            return relativeOrAbsoluteUri;
        }

        return Client.BaseAddress.Combine(relativeOrAbsoluteUri.ToString());
    }

    private static string ApiNameLookup(string serviceRoleName) =>
        serviceRoleName switch
        {
            ServiceRoleName.AC => "Autorisaties API",
            ServiceRoleName.ZTC => "Catalogi API",
            ServiceRoleName.ZRC => "Zaken API",
            ServiceRoleName.DRC => "Documenten API",
            ServiceRoleName.BRC => "Besluiten API",
            ServiceRoleName.NRC => "Notificaties API",
            ServiceRoleName.RL => "Referenties API",
            _ => serviceRoleName,
        };

    protected bool EnsureValidResource(string serviceRoleName, string resourceUrl, string resourceName, out ErrorResponse errorResponse)
    {
        ArgumentNullException.ThrowIfNull(resourceUrl);
        ArgumentNullException.ThrowIfNull(resourceName);

        errorResponse = default;

        var baseAddress = Client.BaseAddress.ToString();

        if (!resourceUrl.StartsWith(baseAddress))
        {
            errorResponse = new ErrorResponse
            {
                Title = $"De URL {resourceUrl} resource is van een externe {ApiNameLookup(serviceRoleName)} en wordt nog niet ondersteund.",
                Code = ErrorCode.BadUrl,
            };
            return false;
        }
        if (!resourceUrl.StartsWith($"{baseAddress.TrimEnd('/')}/{resourceName}"))
        {
            errorResponse = new ErrorResponse
            {
                Title = $"De URL {resourceUrl} is een resource niet voor het ophalen van {resourceName}.",
                Code = ErrorCode.BadUrl,
            };
            return false;
        }
        return true;
    }
}
