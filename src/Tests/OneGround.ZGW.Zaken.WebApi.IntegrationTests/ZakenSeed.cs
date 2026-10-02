using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// The kind of resource seeded under a zaak; a route's <c>{id}</c> or <c>{uuid}</c> segment names one of these.
/// </summary>
internal enum ZaakResource
{
    None,
    Status,
    Resultaat,
    Rol,
    ZaakObject,
    Eigenschap,
    Besluit,
    ZaakInformatieObject,
    Contactmoment,
    Verzoek,
    KlantContact,
    AuditTrailRegel,
}

/// <summary>
/// A zaak written straight into the database, with at most one seeded resource under it.
/// </summary>
internal sealed record SeededZaak(
    Guid Id,
    string Rsin,
    string Identificatie,
    string ZaakType,
    VertrouwelijkheidAanduiding VertrouwelijkheidAanduiding,
    ZaakResource Resource,
    Guid? ResourceId
)
{
    public string Url => ZakenEndpointRequests.Url($"zaken/{Id}");

    public CatalogiZaakType Catalogi => CatalogiStub.Of(ZaakType);
}

internal static class ZakenSeed
{
    /// <summary>
    /// Readies the shared factory for tests that seed and write: answers Catalogi lookups (<see cref="CatalogiStub"/>) and switches notifications
    /// off with the API's own <c>Application:DontSendNotificaties</c> setting, because publishing one sets a RabbitMQ-only message priority the
    /// in-memory bus rejects.
    /// </summary>
    public static void PrepareFactory(ZakenWebApplicationFactory factory)
    {
        CatalogiStub.InstallOn(factory);

        // Handlers read the setting per request, so a write after this one already sees it.
        factory.Services.GetRequiredService<IConfiguration>()["Application:DontSendNotificaties"] = "true";
    }

    /// <summary>
    /// Seeds a zaak of <paramref name="zaakType"/> owned by <paramref name="rsin"/>, closed (with an <c>einddatum</c>) when
    /// <paramref name="closed"/>, and with one <paramref name="resource"/> under it.
    /// </summary>
    public static async Task<SeededZaak> SeedZaakAsync(
        ZakenWebApplicationFactory factory,
        string zaakType = ZaakTypes.A,
        VertrouwelijkheidAanduiding vertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
        bool closed = false,
        ZaakResource resource = ZaakResource.None,
        string rsin = TestRsins.A
    )
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        var catalogi = CatalogiStub.Of(zaakType);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var zaak = new Zaak
        {
            Id = Guid.NewGuid(),
            Owner = rsin,
            Bronorganisatie = rsin,
            VerantwoordelijkeOrganisatie = rsin,
            Identificatie = $"ITEST-{Guid.NewGuid():N}",
            Zaaktype = zaakType,
            Startdatum = today.AddDays(-10),
            Registratiedatum = today.AddDays(-10),
            Einddatum = closed ? today.AddDays(-1) : null,
            Communicatiekanaal = "",
            Selectielijstklasse = "",
            VertrouwelijkheidAanduiding = vertrouwelijkheidAanduiding,
            Archiefnominatie = closed ? ArchiefNominatie.blijvend_bewaren : null,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            CatalogusId = Guid.Parse(CatalogiStub.Catalogus[^36..]),
            LegacyAuditTrail = resource == ZaakResource.AuditTrailRegel,
        };

        context.Zaken.Add(zaak);

        var resourceId = Guid.NewGuid();
        var registered = DateTime.UtcNow.AddDays(-5);

        switch (resource)
        {
            case ZaakResource.None:
                break;

            case ZaakResource.Status:
                context.ZaakStatussen.Add(
                    new ZaakStatus
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        // A closed zaak got its einddatum from a final status.
                        StatusType = closed ? catalogi.EindStatusType : catalogi.BeginStatusType,
                        DatumStatusGezet = registered,
                        StatusToelichting = "",
                        IndicatieLaatstGezetteStatus = true,
                    }
                );
                break;

            case ZaakResource.Resultaat:
                zaak.Resultaat = new ZaakResultaat
                {
                    Id = resourceId,
                    Owner = rsin,
                    Zaak = zaak,
                    ResultaatType = catalogi.ResultaatType,
                    Toelichting = "",
                };
                break;

            case ZaakResource.Rol:
                context.ZaakRollen.Add(
                    new ZaakRol
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        BetrokkeneType = BetrokkeneType.medewerker,
                        RolType = catalogi.RolType,
                        Roltoelichting = "Integratietest",
                        Registratiedatum = registered,
                        Omschrijving = "Behandelaar",
                        OmschrijvingGeneriek = OmschrijvingGeneriek.behandelaar,
                        Medewerker = new MedewerkerZaakRol { Identificatie = "itest-medewerker", Achternaam = "Integratietest" },
                    }
                );
                break;

            case ZaakResource.ZaakObject:
                context.ZaakObjecten.Add(
                    new ZaakObject
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        ObjectType = ObjectType.overige,
                        ObjectTypeOverige = "integratietest",
                        RelatieOmschrijving = "",
                        Overige = new OverigeZaakObject { Owner = rsin, OverigeData = """{"kenmerk":"integratietest"}""" },
                    }
                );
                break;

            case ZaakResource.Eigenschap:
                context.ZaakEigenschappen.Add(
                    new ZaakEigenschap
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        Eigenschap = catalogi.Eigenschap,
                        Naam = "kenmerk",
                        Waarde = "seeded",
                    }
                );
                break;

            case ZaakResource.Besluit:
                context.ZaakBesluiten.Add(
                    new ZaakBesluit
                    {
                        Id = resourceId,
                        Zaak = zaak,
                        Besluit = ZakenEndpointRequests.BesluitUrl,
                    }
                );
                break;

            case ZaakResource.ZaakInformatieObject:
                context.ZaakInformatieObjecten.Add(
                    new ZaakInformatieObject
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        InformatieObject = ZakenEndpointRequests.InformatieObjectUrl,
                        AardRelatieWeergave = AardRelatieWeergave.hoort_bij_omgekeerd_kent,
                        RegistratieDatum = registered,
                        Titel = "Integratietest",
                        Beschrijving = "",
                    }
                );
                break;

            case ZaakResource.Contactmoment:
                context.ZaakContactmomenten.Add(
                    new ZaakContactmoment
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        Contactmoment = ZakenEndpointRequests.ContactmomentUrl,
                    }
                );
                break;

            case ZaakResource.Verzoek:
                context.ZaakVerzoeken.Add(
                    new ZaakVerzoek
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        Verzoek = ZakenEndpointRequests.VerzoekUrl,
                    }
                );
                break;

            case ZaakResource.KlantContact:
                context.KlantContacten.Add(
                    new KlantContact
                    {
                        Id = resourceId,
                        Zaak = zaak,
                        Identificatie = "ITEST",
                        DatumTijd = registered,
                        Kanaal = "telefoon",
                        Onderwerp = "Integratietest",
                        Toelichting = "",
                    }
                );
                break;

            case ZaakResource.AuditTrailRegel:
                context.AuditTrailRegels.Add(
                    new AuditTrailRegel
                    {
                        Id = resourceId,
                        Bron = "ZRC",
                        ApplicatieId = "integration-test",
                        ApplicatieWeergave = "integration-test",
                        GebruikersId = "integration-test",
                        GebruikersWeergave = "integration-test",
                        Actie = "create",
                        ActieWeergave = "Object aangemaakt",
                        Resultaat = 201,
                        HoofdObject = ZakenEndpointRequests.Url($"zaken/{zaak.Id}"),
                        HoofdObjectId = zaak.Id,
                        Resource = "zaak",
                        ResourceUrl = ZakenEndpointRequests.Url($"zaken/{zaak.Id}"),
                        ResourceWeergave = zaak.Identificatie,
                        Toelichting = "",
                        AanmaakDatum = registered,
                    }
                );
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(resource), resource, null);
        }

        await context.SaveChangesAsync();

        return new SeededZaak(
            zaak.Id,
            rsin,
            zaak.Identificatie,
            zaakType,
            vertrouwelijkheidAanduiding,
            resource,
            resource == ZaakResource.None ? null : resourceId
        );
    }
}
