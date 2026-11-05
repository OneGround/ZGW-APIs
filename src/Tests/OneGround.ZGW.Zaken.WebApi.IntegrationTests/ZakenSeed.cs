using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    Notitie,
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
    public string Url => ZakenEndpointRequests.ZaakUrl(Id);

    public CatalogiZaakType Catalogi => CatalogiStub.Of(ZaakType);
}

internal static class ZakenSeed
{
    /// <summary>
    /// Seeds a zaak of <paramref name="zaakType"/> for <paramref name="rsin"/>, optionally closed, with one <paramref name="resource"/>, a hoofdzaak and a relevante andere zaak.
    /// </summary>
    public static async Task<SeededZaak> SeedZaakAsync(
        ZakenWebApplicationFactory factory,
        string zaakType = ZaakTypes.A,
        VertrouwelijkheidAanduiding vertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
        bool closed = false,
        ZaakResource resource = ZaakResource.None,
        string rsin = TestRsins.A,
        Guid? hoofdzaakId = null,
        Guid? relevanteAndereZaakId = null
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
            // Only a legacy zaak reads its audit trail from the AuditTrailRegels table, where the regel is seeded.
            LegacyAuditTrail = resource == ZaakResource.AuditTrailRegel,
            HoofdzaakId = hoofdzaakId,
        };

        if (relevanteAndereZaakId is { } relevant)
        {
            zaak.RelevanteAndereZaken =
            [
                new RelevanteAndereZaak
                {
                    Id = Guid.NewGuid(),
                    Owner = rsin,
                    Url = ZakenEndpointRequests.ZaakUrl(relevant),
                    AardRelatie = "vervolg",
                },
            ];
        }

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
                        HoofdObject = ZakenEndpointRequests.ZaakUrl(zaak.Id),
                        HoofdObjectId = zaak.Id,
                        Resource = "zaak",
                        ResourceUrl = ZakenEndpointRequests.ZaakUrl(zaak.Id),
                        ResourceWeergave = zaak.Identificatie,
                        Toelichting = "",
                        AanmaakDatum = registered,
                    }
                );
                break;

            case ZaakResource.Notitie:
                context.ZaakNotities.Add(
                    new ZaakNotitie
                    {
                        Id = resourceId,
                        Owner = rsin,
                        Zaak = zaak,
                        CreationTime = registered,
                        Onderwerp = "Integratietest",
                        Tekst = "Integratietest",
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

    public static async Task<bool> ZaakExistsAsync(ZakenWebApplicationFactory factory, Guid zaakId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.Zaken.AnyAsync(z => z.Id == zaakId);
    }

    /// <summary>
    /// The stored columns of the zaak and of every row under it, one line per row, to compare before and after a request.
    /// </summary>
    public static async Task<string> ZaakStateAsync(ZakenWebApplicationFactory factory, Guid zaakId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        var rows = new List<object>();
        rows.AddRange(await context.Zaken.AsNoTracking().Where(z => z.Id == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakStatussen.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakRollen.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakResultaten.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakObjecten.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakEigenschappen.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakBesluiten.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakInformatieObjecten.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakContactmomenten.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakVerzoeken.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.KlantContacten.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());
        rows.AddRange(await context.ZaakNotities.AsNoTracking().Where(r => r.ZaakId == zaakId).ToListAsync());

        return string.Join(Environment.NewLine, rows.Select(row => Describe(context, row)).Order(StringComparer.Ordinal));
    }

    private static string Describe(ZrcDbContext context, object row)
    {
        var entityType = context.Model.FindEntityType(row.GetType())!;
        var columns = entityType.GetProperties().Select(p => $"{p.Name}={p.PropertyInfo?.GetValue(row) ?? p.FieldInfo?.GetValue(row)}");

        return $"{entityType.ShortName()}: {string.Join(", ", columns)}";
    }

    public static async Task<int> ZaakStatusCountAsync(ZakenWebApplicationFactory factory, Guid zaakId)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        return await context.ZaakStatussen.CountAsync(s => s.ZaakId == zaakId);
    }
}
