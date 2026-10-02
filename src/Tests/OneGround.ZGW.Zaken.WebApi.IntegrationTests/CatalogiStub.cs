using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Http;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// The Catalogi resources of one made-up zaaktype: a non-final and a final statustype, and one resultaattype, roltype and eigenschap.
/// </summary>
internal sealed record CatalogiZaakType(
    string Url,
    string BeginStatusType,
    string EindStatusType,
    string ResultaatType,
    string RolType,
    string Eigenschap
);

/// <summary>
/// Answers the Catalogi lookups the Zaken handlers make for <see cref="ZaakTypes.A"/> and <see cref="ZaakTypes.B"/>, on fixed <c>.invalid</c> URLs.
/// </summary>
internal static class CatalogiStub
{
    private const string BaseUrl = "https://catalogi.integrationtest.invalid/api/v1";

    public const string Catalogus = BaseUrl + "/catalogussen/3f1c2a5e-0000-4000-8000-0000000000c0";

    // The type of the document behind ZakenEndpointRequests.InformatieObjectUrl, which both zaaktypes allow.
    public const string InformatieObjectType = BaseUrl + "/informatieobjecttypen/3f1c2a5e-0000-4000-8000-0000000000d0";

    public static readonly CatalogiZaakType A = Create(ZaakTypes.A, 'a');

    public static readonly CatalogiZaakType B = Create(ZaakTypes.B, 'b');

    private static readonly Dictionary<string, object> Resources = CreateResources();

    private static readonly ConditionalWeakTable<StubOutboundHttp, object> Installed = new();

    public static CatalogiZaakType Of(string zaakType)
    {
        return zaakType switch
        {
            ZaakTypes.A => A,
            ZaakTypes.B => B,
            _ => throw new ArgumentOutOfRangeException(nameof(zaakType), zaakType, "No Catalogi stub for this zaaktype."),
        };
    }

    /// <summary>
    /// Chains the stub, and <see cref="DocumentenStub"/> after it, after the factory's own responder, once per factory, so a request neither of them answers still fails.
    /// </summary>
    public static void InstallOn(ZakenWebApplicationFactory factory)
    {
        var outboundHttp = factory.OutboundHttp;

        lock (Installed)
        {
            if (Installed.TryGetValue(outboundHttp, out _))
                return;

            var previous = outboundHttp.Responder;
            outboundHttp.Responder = request => previous?.Invoke(request) ?? TryRespond(request) ?? DocumentenStub.TryRespond(request);

            Installed.Add(outboundHttp, null);
        }
    }

    /// <summary>
    /// The resource a GET asks for, a 404 for any other GET on the Catalogi API, or <c>null</c> for a request to another host.
    /// </summary>
    public static HttpResponseMessage TryRespond(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        if (!uri.GetLeftPart(UriPartial.Path).StartsWith(BaseUrl + "/", StringComparison.OrdinalIgnoreCase))
            return null;

        if (request.Method != HttpMethod.Get || !Resources.TryGetValue(uri.GetLeftPart(UriPartial.Path), out var resource))
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(resource), Encoding.UTF8, "application/json"),
        };
    }

    private static CatalogiZaakType Create(string zaakType, char suffix)
    {
        return new CatalogiZaakType(
            zaakType,
            BeginStatusType: $"{BaseUrl}/statustypen/3f1c2a5e-0001-4000-8000-00000000000{suffix}",
            EindStatusType: $"{BaseUrl}/statustypen/3f1c2a5e-0002-4000-8000-00000000000{suffix}",
            ResultaatType: $"{BaseUrl}/resultaattypen/3f1c2a5e-0003-4000-8000-00000000000{suffix}",
            RolType: $"{BaseUrl}/roltypen/3f1c2a5e-0004-4000-8000-00000000000{suffix}",
            Eigenschap: $"{BaseUrl}/eigenschappen/3f1c2a5e-0005-4000-8000-00000000000{suffix}"
        );
    }

    private static Dictionary<string, object> CreateResources()
    {
        var resources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [Catalogus] = new CatalogusResponseDto
            {
                Url = Catalogus,
                Domein = "ITEST",
                Rsin = TestRsins.A,
                ZaakTypen = [A.Url, B.Url],
                BesluitTypen = [],
                InformatieObjectTypen = [],
            },
        };

        foreach (var zaakType in new[] { A, B })
        {
            var identificatie = zaakType == A ? "ITEST-A" : "ITEST-B";

            resources[zaakType.Url] = new ZaakTypeResponseDto
            {
                Url = zaakType.Url,
                Identificatie = identificatie,
                Omschrijving = $"Integratietest zaaktype {identificatie}",
                VertrouwelijkheidAanduiding = "openbaar",
                Doorlooptijd = "P30D",
                Catalogus = Catalogus,
                BeginGeldigheid = "2020-01-01",
                VersieDatum = "2020-01-01",
                Concept = false,
                Trefwoorden = [],
                Verantwoordingsrelatie = [],
                ProductenOfDiensten = [],
                BesluitTypen = [],
                DeelZaakTypen = [],
                GerelateerdeZaakTypen = [],
                StatusTypen = [zaakType.BeginStatusType, zaakType.EindStatusType],
                ResultaatTypen = [zaakType.ResultaatType],
                Eigenschappen = [zaakType.Eigenschap],
                InformatieObjectTypen = [InformatieObjectType],
                RolTypen = [zaakType.RolType],
            };

            resources[zaakType.BeginStatusType] = new StatusTypeResponseDto
            {
                Url = zaakType.BeginStatusType,
                ZaakType = zaakType.Url,
                Omschrijving = "Ontvangen",
                VolgNummer = 1,
                IsEindStatus = false,
            };

            resources[zaakType.EindStatusType] = new StatusTypeResponseDto
            {
                Url = zaakType.EindStatusType,
                ZaakType = zaakType.Url,
                Omschrijving = "Afgehandeld",
                VolgNummer = 2,
                IsEindStatus = true,
            };

            resources[zaakType.ResultaatType] = new ResultaatTypeResponseDto
            {
                Url = zaakType.ResultaatType,
                ZaakType = zaakType.Url,
                Omschrijving = "Toegekend",
                ArchiefNominatie = "blijvend_bewaren",
                ArchiefActieTermijn = "P10Y",
                BronDatumArchiefProcedure = new BronDatumArchiefProcedureDto { Afleidingswijze = "afgehandeld" },
            };

            resources[zaakType.RolType] = new RolTypeResponseDto
            {
                Url = zaakType.RolType,
                ZaakType = zaakType.Url,
                Omschrijving = "Behandelaar",
                OmschrijvingGeneriek = "behandelaar",
            };

            resources[zaakType.Eigenschap] = new EigenschapResponseDto
            {
                Url = zaakType.Eigenschap,
                ZaakType = zaakType.Url,
                Naam = "kenmerk",
                Definitie = "Integratietest eigenschap",
                Specificatie = new EigenschapSpecificatieDto
                {
                    Formaat = "tekst",
                    Lengte = "100",
                    Kardinaliteit = "1",
                    Waardenverzameling = [],
                },
            };
        }

        return resources;
    }
}

/// <summary>
/// Answers the Documenten lookup of <see cref="ZakenEndpointRequests.InformatieObjectUrl"/>, which a zaakinformatieobject create makes before
/// its handler runs; any other request to the Documenten API gets a 404.
/// </summary>
internal static class DocumentenStub
{
    private const string BaseUrl = "https://documenten.integrationtest.invalid/api/v1";

    public static HttpResponseMessage TryRespond(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        if (!uri.GetLeftPart(UriPartial.Path).StartsWith(BaseUrl + "/", StringComparison.OrdinalIgnoreCase))
            return null;

        if (request.Method != HttpMethod.Get || uri.GetLeftPart(UriPartial.Path) != ZakenEndpointRequests.InformatieObjectUrl)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        var informatieObject = new
        {
            url = ZakenEndpointRequests.InformatieObjectUrl,
            bronorganisatie = TestRsins.A,
            informatieobjecttype = CatalogiStub.InformatieObjectType,
            titel = "Integratietest",
            vertrouwelijkheidaanduiding = "openbaar",
            indicatieGebruiksrecht = false,
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(informatieObject), Encoding.UTF8, "application/json"),
        };
    }
}
