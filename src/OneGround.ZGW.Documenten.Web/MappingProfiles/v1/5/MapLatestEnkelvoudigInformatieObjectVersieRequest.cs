using AutoMapper;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

public class MapLatestEnkelvoudigInformatieObjectVersieRequest
    : IMappingAction<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectUpdateRequestDto>
{
    public void Process(EnkelvoudigInformatieObject src, EnkelvoudigInformatieObjectUpdateRequestDto dest, ResolutionContext context)
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

        dest.Trefwoorden = latestVersion.Trefwoorden;
        dest.InhoudIsVervallen = latestVersion.InhoudIsVervallen;

        // Note: Don't merge the lock value because we have to validate the value from request and not the one in the database after the merge)
        //   (meaning don't: dest.Lock = latestVersion.LatestInformatieObject.Lock)
    }
}
