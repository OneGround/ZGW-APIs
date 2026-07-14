using Mapster;
using Newtonsoft.Json;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

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
            .Map(dest => dest.Inhoud, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObject.InformatieObjectType);

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
            .Map(dest => dest.Inhoud, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObject.InformatieObjectType);

        // Note: This map is used to merge an existing ENKELVOUDIGINFORMATIEOBJECT(+VERSIE) with the PATCH operation
        config
            .NewConfig<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectUpdateRequestDto>()
            .Ignore(dest => dest.Identificatie)
            .Ignore(dest => dest.Bronorganisatie)
            .Ignore(dest => dest.CreatieDatum)
            .Ignore(dest => dest.Titel)
            .Ignore(dest => dest.Vertrouwelijkheidaanduiding)
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
            .AfterMapping((src, dest) => MapLatestVersieToUpdateRequest(src, dest));

        config
            .NewConfig<ObjectInformatieObject, ObjectInformatieObjectResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.InformatieObject, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject));

        config
            .NewConfig<GebruiksRecht, GebruiksRechtResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.InformatieObject, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDateTime(src.Startdatum, true))
            .Map(dest => dest.Einddatum, src => ProfileHelper.StringDateFromDateTime(src.Einddatum, true));

        // Note: This map is used to merge an existing GEBRUIKSRECHT with the PATCH operation
        config
            .NewConfig<GebruiksRecht, GebruiksRechtRequestDto>()
            .Map(dest => dest.InformatieObject, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDateTime(src.Startdatum, true))
            .Map(dest => dest.Einddatum, src => ProfileHelper.StringDateFromDateTime(src.Einddatum, true));

        config
            .NewConfig<AuditTrailRegel, AuditTrailRegelDto>()
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Wijzigingen, src => ConvertWijzigingenToDto(src.Oud, src.Nieuw))
            .Map(dest => dest.AanmaakDatum, src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true));
    }

    // Ported verbatim from MapLatestEnkelvoudigInformatieObjectVersieResponse.Process(...) (_uriService -> uriService parameter).
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
        dest.Auteur = latestVersion.Auteur;
        dest.Status = $"{latestVersion.Status}";
        dest.Formaat = latestVersion.Formaat;
        dest.Taal = latestVersion.Taal;
        dest.Bestandsnaam = latestVersion.Bestandsnaam;
        dest.Link = latestVersion.Link;
        dest.Inhoud = uriService.GetUri(latestVersion);
        dest.Beschrijving = latestVersion.Beschrijving;
        dest.OntvangstDatum = ProfileHelper.StringDateFromDate(latestVersion.OntvangstDatum);
        dest.VerzendDatum = ProfileHelper.StringDateFromDate(latestVersion.VerzendDatum);
        dest.Ondertekening = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(latestVersion, true);
        dest.Integriteit = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(latestVersion, true);

        dest.InformatieObjectType = latestVersion.LatestInformatieObject.InformatieObjectType;
        dest.IndicatieGebruiksrecht = latestVersion.LatestInformatieObject.IndicatieGebruiksrecht;
        dest.Locked = latestVersion.LatestInformatieObject.Locked;
    }

    // Ported verbatim from MapLatestEnkelvoudigInformatieObjectVersieRequest.Process(...).
    private static void MapLatestVersieToUpdateRequest(EnkelvoudigInformatieObject src, EnkelvoudigInformatieObjectUpdateRequestDto dest)
    {
        // Note: For update-request-mapping we get always get the latest version
        var latestVersion = src.LatestEnkelvoudigInformatieObjectVersie;

        dest.Bronorganisatie = latestVersion.Bronorganisatie;
        dest.Identificatie = latestVersion.Identificatie;
        dest.CreatieDatum = ProfileHelper.StringDateFromDate(latestVersion.CreatieDatum);
        dest.Titel = latestVersion.Titel;
        dest.Vertrouwelijkheidaanduiding = $"{latestVersion.Vertrouwelijkheidaanduiding}";
        dest.Auteur = latestVersion.Auteur;
        dest.Status = $"{latestVersion.Status}";
        dest.Formaat = latestVersion.Formaat;
        dest.Taal = latestVersion.Taal;
        dest.Bestandsnaam = latestVersion.Bestandsnaam;
        dest.Inhoud = latestVersion.Inhoud;
        dest.Link = latestVersion.Link;
        dest.Beschrijving = latestVersion.Beschrijving;
        dest.OntvangstDatum = ProfileHelper.StringDateFromDate(latestVersion.OntvangstDatum);
        dest.VerzendDatum = ProfileHelper.StringDateFromDate(latestVersion.VerzendDatum);
        dest.Ondertekening = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(latestVersion, false);
        dest.Integriteit = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(latestVersion, false);
        dest.InformatieObjectType = latestVersion.LatestInformatieObject.InformatieObjectType;
        dest.IndicatieGebruiksrecht = latestVersion.LatestInformatieObject.IndicatieGebruiksrecht;

        // Note: Don't merge the lock value because we have to validate the value from request and not the one in the database after the merge)
        //   (meaning don't: dest.Lock = latestVersion.LatestInformatieObject.Lock)
    }

    private static WijzigingDto ConvertWijzigingenToDto(string oud, string nieuw)
    {
        var result = new WijzigingDto();

        if (!string.IsNullOrEmpty(oud))
        {
            result.Oud = JsonConvert.DeserializeObject(oud, new ZGWJsonSerializerSettings());
        }
        if (!string.IsNullOrEmpty(nieuw))
        {
            result.Nieuw = JsonConvert.DeserializeObject(nieuw, new ZGWJsonSerializerSettings());
        }
        return result;
    }
}
