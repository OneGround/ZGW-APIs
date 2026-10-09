using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Responses;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.IntegrationTests.Common.Authorization;

/// <summary>
/// Answers the API's outbound applicatie lookups on the Autorisaties API with the applicaties a test registered per client id.
/// </summary>
public sealed class StubAutorisatiesApi
{
    private readonly ConcurrentDictionary<string, ApplicatieResponseDto> _applicaties = new();
    private readonly string _consumerEndpoint;

    public StubAutorisatiesApi(string baseUrl)
    {
        _consumerEndpoint = baseUrl.TrimEnd('/') + "/applicaties/consumer";
    }

    public void GrantNoAuthorizations(string clientId)
    {
        Set(clientId, heeftAlleAutorisaties: false, []);
    }

    public void GrantAllAuthorizations(string clientId)
    {
        Set(clientId, heeftAlleAutorisaties: true, []);
    }

    public void Grant(string clientId, params AutorisatieResponseDto[] autorisaties)
    {
        Set(clientId, heeftAlleAutorisaties: false, autorisaties);
    }

    /// <summary>
    /// The response to an applicatie lookup, a 404 for an unregistered client id, or <c>null</c> for any other request.
    /// </summary>
    public HttpResponseMessage TryRespond(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        if (request.Method != HttpMethod.Get || !uri.GetLeftPart(UriPartial.Path).Equals(_consumerEndpoint, StringComparison.OrdinalIgnoreCase))
            return null;

        var clientId = QueryHelpers.ParseQuery(uri.Query).TryGetValue("clientId", out var values) ? values.ToString() : null;

        if (clientId == null || !_applicaties.TryGetValue(clientId, out var applicatie))
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(applicatie), Encoding.UTF8, "application/json"),
        };
    }

    private void Set(string clientId, bool heeftAlleAutorisaties, AutorisatieResponseDto[] autorisaties)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        _applicaties[clientId] = new ApplicatieResponseDto
        {
            Url = $"{_consumerEndpoint}/{Guid.NewGuid()}",
            Label = clientId,
            ClientIds = [clientId],
            HeeftAlleAutorisaties = heeftAlleAutorisaties,
            Autorisaties = new List<AutorisatieResponseDto>(autorisaties),
        };
    }
}
