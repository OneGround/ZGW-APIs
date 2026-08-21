using System.Linq;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._7;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectGetResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Ignore(dest => dest.Versie)
            .Ignore(dest => dest.BeginRegistratie)
            .Ignore(dest => dest.Bestandsomvang)
            .Ignore(dest => dest.Identificatie)
            .Ignore(dest => dest.Bronorganisatie)
            .Ignore(dest => dest.CreatieDatum)
            .Ignore(dest => dest.Titel)
            .Ignore(dest => dest.Vertrouwelijkheidaanduiding)
            .Ignore(dest => dest.IsGereedVoorPublicatie)
            .Ignore(dest => dest.TonenAanInitiator)
            .Ignore(dest => dest.Auteur)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.Formaat)
            .Ignore(dest => dest.Taal)
            .Ignore(dest => dest.Bestandsnaam)
            .Ignore(dest => dest.Inhoud)
            .Ignore(dest => dest.Link)
            .Ignore(dest => dest.Beschrijving)
            .Ignore(dest => dest.OntvangstDatum)
            .Ignore(dest => dest.VerzendDatum)
            .Ignore(dest => dest.IndicatieGebruiksrecht)
            .Ignore(dest => dest.Ondertekening)
            .Ignore(dest => dest.Integriteit)
            .Ignore(dest => dest.Verschijningsvorm)
            .Ignore(dest => dest.Trefwoorden)
            // Set below in MapLatestVersieToGetResponse's AfterMapping, invisible to Mapster's static analysis.
            .Ignore(dest => dest.BestandsDelen)
            .Ignore(dest => dest.InhoudIsVervallen)
            .AfterMapping((src, dest) => MapLatestVersieToGetResponse(src, dest, MapContext.Current.GetService<IEntityUriService>()));

        config
            .NewConfig<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectCreateResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject))
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.StringDateFromDate(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.StringDateFromDate(src.OntvangstDatum))
            .Map(dest => dest.BeginRegistratie, src => ProfileHelper.StringDateFromDateTime(src.BeginRegistratie, true))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.StringDateFromDate(src.VerzendDatum))
            .Map(dest => dest.Ondertekening, src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(src, true))
            .Map(dest => dest.Integriteit, src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(src, true))
            .Map(dest => dest.IndicatieGebruiksrecht, src => src.InformatieObject.IndicatieGebruiksrecht)
            .Map(dest => dest.Locked, src => src.InformatieObject.Locked)
            .Map(dest => dest.Lock, src => src.InformatieObject.Lock)
            .Map(dest => dest.Verschijningsvorm, src => src.Verschijningsvorm)
            .Map(dest => dest.Trefwoorden, src => src.Trefwoorden)
            .Ignore(dest => dest.Inhoud)
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObject.InformatieObjectType)
            .AfterMapping((src, dest) => MapDownloadLink(src, dest, MapContext.Current.GetService<IEntityUriService>()));

        config
            .NewConfig<EnkelvoudigInformatieObjectVersie, EnkelvoudigInformatieObjectUpdateResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject))
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.StringDateFromDate(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.StringDateFromDate(src.OntvangstDatum))
            .Map(dest => dest.BeginRegistratie, src => ProfileHelper.StringDateFromDateTime(src.BeginRegistratie, true))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.StringDateFromDate(src.VerzendDatum))
            .Map(dest => dest.Ondertekening, src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(src, true))
            .Map(dest => dest.Integriteit, src => EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(src, true))
            .Map(dest => dest.IndicatieGebruiksrecht, src => src.InformatieObject.IndicatieGebruiksrecht)
            .Map(dest => dest.Locked, src => src.InformatieObject.Locked)
            .Map(dest => dest.Lock, src => src.InformatieObject.Lock)
            .Map(dest => dest.Verschijningsvorm, src => src.Verschijningsvorm)
            .Map(dest => dest.Trefwoorden, src => src.Trefwoorden)
            .Ignore(dest => dest.Inhoud)
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObject.InformatieObjectType)
            .AfterMapping((src, dest) => MapDownloadLink(src, dest, MapContext.Current.GetService<IEntityUriService>()));

        // Note: This map is used to merge an existing ENKELVOUDIGINFORMATIEOBJECT(+VERSIE) with the PATCH operation
        config
            .NewConfig<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectUpdateRequestDto>()
            .Ignore(dest => dest.Identificatie)
            .Ignore(dest => dest.Bronorganisatie)
            .Ignore(dest => dest.CreatieDatum)
            .Ignore(dest => dest.Titel)
            .Ignore(dest => dest.Vertrouwelijkheidaanduiding)
            .Ignore(dest => dest.IsGereedVoorPublicatie)
            .Ignore(dest => dest.TonenAanInitiator)
            .Ignore(dest => dest.Auteur)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.Formaat)
            .Ignore(dest => dest.Taal)
            .Ignore(dest => dest.Bestandsnaam)
            .Ignore(dest => dest.Inhoud)
            .Ignore(dest => dest.Link)
            .Ignore(dest => dest.Beschrijving)
            .Ignore(dest => dest.OntvangstDatum)
            .Ignore(dest => dest.VerzendDatum)
            .Ignore(dest => dest.IndicatieGebruiksrecht)
            .Ignore(dest => dest.Ondertekening)
            .Ignore(dest => dest.Integriteit)
            .Ignore(dest => dest.Lock) // Note: Don't merge the lock value because we have to validate the value from request and not the one in the database after the merge)
            // Set below in MapLatestVersieToUpdateRequest's AfterMapping, invisible to Mapster's static analysis.
            .Ignore(dest => dest.Bestandsomvang)
            .Ignore(dest => dest.Verschijningsvorm)
            .Ignore(dest => dest.InhoudIsVervallen)
            .Ignore(dest => dest.Trefwoorden)
            .AfterMapping((src, dest) => MapLatestVersieToUpdateRequest(src, dest));
    }

    // Ported verbatim from v1.7's MapLatestEnkelvoudigInformatieObjectVersieResponse.Process(...) (_uriService -> uriService parameter).
    private static void MapLatestVersieToGetResponse(
        EnkelvoudigInformatieObject src,
        EnkelvoudigInformatieObjectGetResponseDto dest,
        IEntityUriService uriService
    )
    {
        // Note: For update-request-mapping we get always get the latest version
        var latestVersion = src.LatestEnkelvoudigInformatieObjectVersie;

        dest.Versie = latestVersion.Versie;
        dest.Bronorganisatie = latestVersion.Bronorganisatie;
        dest.Identificatie = latestVersion.Identificatie;
        dest.Bestandsomvang = latestVersion.Bestandsomvang;
        dest.BeginRegistratie = ProfileHelper.StringDateFromDateTime(latestVersion.BeginRegistratie, withTime: true);
        dest.CreatieDatum = ProfileHelper.StringDateFromDate(latestVersion.CreatieDatum);
        dest.Titel = latestVersion.Titel;
        dest.Vertrouwelijkheidaanduiding = $"{latestVersion.Vertrouwelijkheidaanduiding}";
        dest.IsGereedVoorPublicatie = latestVersion.IsGereedVoorPublicatie;
        dest.TonenAanInitiator = latestVersion.TonenAanInitiator;
        dest.Auteur = latestVersion.Auteur;
        dest.Status = $"{latestVersion.Status}";
        dest.Formaat = latestVersion.Formaat;
        dest.Taal = latestVersion.Taal;
        dest.Bestandsnaam = latestVersion.Bestandsnaam;
        dest.Bestandsomvang = latestVersion.Bestandsomvang;
        dest.Link = latestVersion.Link;
        dest.Inhoud = latestVersion.BestandsDelen.Count != 0 ? null : uriService.GetUri(latestVersion);
        dest.Beschrijving = latestVersion.Beschrijving;
        dest.OntvangstDatum = ProfileHelper.StringDateFromDate(latestVersion.OntvangstDatum);
        dest.VerzendDatum = ProfileHelper.StringDateFromDate(latestVersion.VerzendDatum);
        dest.Ondertekening = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(latestVersion, true);
        dest.Integriteit = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(latestVersion, true);

        dest.InformatieObjectType = latestVersion.LatestInformatieObject.InformatieObjectType;
        dest.IndicatieGebruiksrecht = latestVersion.LatestInformatieObject.IndicatieGebruiksrecht;
        dest.Locked = latestVersion.LatestInformatieObject.Locked;

        dest.Verschijningsvorm = latestVersion.Verschijningsvorm;
        dest.Trefwoorden = latestVersion.Trefwoorden;
        dest.InhoudIsVervallen = latestVersion.InhoudIsVervallen;

        dest.BestandsDelen = latestVersion
            .BestandsDelen.OrderBy(d => d.Volgnummer)
            .Select(bestandsdeel => MapBestandsDeel(bestandsdeel, uriService))
            .ToList();
    }

    // Ported verbatim from v1.7's MapLatestEnkelvoudigInformatieObjectVersieResponse.MapBestandsDeel(...) (_uriService -> uriService parameter).
    private static Documenten.Contracts.v1._1.Responses.BestandsDeelResponseDto MapBestandsDeel(
        BestandsDeel bestandsdeel,
        IEntityUriService uriService
    )
    {
        var uri = uriService.GetUri(bestandsdeel);

        return new Documenten.Contracts.v1._1.Responses.BestandsDeelResponseDto
        {
            Url = uri,
            Omvang = bestandsdeel.Omvang,
            Volgnummer = bestandsdeel.Volgnummer,
            Voltooid = bestandsdeel.Voltooid,
            Lock = bestandsdeel.EnkelvoudigInformatieObjectVersie.LatestInformatieObject.Lock,
        };
    }

    // Ported verbatim from v1.7's MapDownloadLink.Process(...) (_uriService -> uriService parameter).
    // Applied at both the Create-response and Update-response configs, which share this common base destination type.
    private static void MapDownloadLink(
        EnkelvoudigInformatieObjectVersie src,
        EnkelvoudigInformatieObjectResponseDto dest,
        IEntityUriService uriService
    )
    {
        if ((string.IsNullOrEmpty(src.Inhoud) && src.Bestandsomvang == 0) || src.BestandsDelen.Count != 0) // Note: New in v1.1
            dest.Inhoud = null;
        else
            dest.Inhoud = uriService.GetUri(src);
    }

    // Ported verbatim from v1.7's MapLatestEnkelvoudigInformatieObjectVersieRequest.Process(...). No DI dependency.
    private static void MapLatestVersieToUpdateRequest(EnkelvoudigInformatieObject src, EnkelvoudigInformatieObjectUpdateRequestDto dest)
    {
        // Note: For update-request-mapping we get always get the latest version
        var latestVersion = src.LatestEnkelvoudigInformatieObjectVersie;

        dest.Bronorganisatie = latestVersion.Bronorganisatie;
        dest.Identificatie = latestVersion.Identificatie;
        dest.CreatieDatum = ProfileHelper.StringDateFromDate(latestVersion.CreatieDatum);
        dest.Titel = latestVersion.Titel;
        dest.Vertrouwelijkheidaanduiding = $"{latestVersion.Vertrouwelijkheidaanduiding}";
        dest.IsGereedVoorPublicatie = latestVersion.IsGereedVoorPublicatie;
        dest.TonenAanInitiator = latestVersion.TonenAanInitiator;
        dest.Auteur = latestVersion.Auteur;
        dest.Status = $"{latestVersion.Status}";
        dest.Formaat = latestVersion.Formaat;
        dest.Taal = latestVersion.Taal;
        dest.Bestandsnaam = latestVersion.Bestandsnaam;
        dest.Bestandsomvang = latestVersion.Bestandsomvang;
        dest.Inhoud = latestVersion.Inhoud;
        dest.Link = latestVersion.Link;
        dest.Beschrijving = latestVersion.Beschrijving;
        dest.OntvangstDatum = ProfileHelper.StringDateFromDate(latestVersion.OntvangstDatum);
        dest.VerzendDatum = ProfileHelper.StringDateFromDate(latestVersion.VerzendDatum);
        dest.Ondertekening = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(latestVersion, false);
        dest.Integriteit = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(latestVersion, false);
        dest.InformatieObjectType = latestVersion.LatestInformatieObject.InformatieObjectType;
        dest.IndicatieGebruiksrecht = latestVersion.LatestInformatieObject.IndicatieGebruiksrecht;

        dest.Verschijningsvorm = latestVersion.Verschijningsvorm;
        dest.Trefwoorden = latestVersion.Trefwoorden;
        dest.InhoudIsVervallen = latestVersion.InhoudIsVervallen;

        // Note: Don't merge the lock value because we have to validate the value from request and not the one in the database after the merge)
        //   (meaning don't: dest.Lock = latestVersion.LatestInformatieObject.Lock)
    }
}
