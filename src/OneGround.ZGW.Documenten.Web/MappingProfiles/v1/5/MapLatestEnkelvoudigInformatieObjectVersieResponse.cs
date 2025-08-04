using System.Linq;
using AutoMapper;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

public class MapLatestEnkelvoudigInformatieObjectVersieResponse
    : IMappingAction<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectGetResponseDto>
{
    private readonly IEntityUriService _uriService;

    public MapLatestEnkelvoudigInformatieObjectVersieResponse(IEntityUriService uriService)
    {
        _uriService = uriService;
    }

    public void Process(EnkelvoudigInformatieObject src, EnkelvoudigInformatieObjectGetResponseDto dest, ResolutionContext context)
    {
        // Note: For update-request-mapping we get always get the latest version
        var latestVersion = src.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).LastOrDefault();
        if (latestVersion == null)
        {
            return;
        }

        dest.Versie = latestVersion.Versie;
        dest.Bronorganisatie = latestVersion.Bronorganisatie;
        dest.Identificatie = latestVersion.Identificatie;
        dest.Bestandsomvang = latestVersion.Bestandsomvang;
        dest.BeginRegistratie = latestVersion.BeginRegistratie;
        dest.CreatieDatum = ProfileHelper.StringDateFromDate(latestVersion.CreatieDatum);
        dest.Titel = latestVersion.Titel;
        dest.Vertrouwelijkheidaanduiding = $"{latestVersion.Vertrouwelijkheidaanduiding}";
        dest.Auteur = latestVersion.Auteur;
        dest.Status = $"{latestVersion.Status}";
        dest.Formaat = latestVersion.Formaat;
        dest.Taal = latestVersion.Taal;
        dest.Bestandsnaam = latestVersion.Bestandsnaam;
        dest.Bestandsomvang = latestVersion.Bestandsomvang;
        dest.Link = latestVersion.Link;
        dest.Inhoud = latestVersion.BestandsDelen.Count != 0 ? null : _uriService.GetUri(latestVersion);
        dest.Beschrijving = latestVersion.Beschrijving;
        dest.OntvangstDatum = ProfileHelper.StringDateFromDate(latestVersion.OntvangstDatum);
        dest.VerzendDatum = ProfileHelper.StringDateFromDate(latestVersion.VerzendDatum);
        dest.Ondertekening = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalOndertekeningDto(latestVersion, true);
        dest.Integriteit = EnkelvoudigInformatieObjectVersieMapperHelper.CreateOptionalIntegriteitDto(latestVersion, true);
        dest.InformatieObjectType = latestVersion.InformatieObject.InformatieObjectType;
        dest.IndicatieGebruiksrecht = latestVersion.InformatieObject.IndicatieGebruiksrecht;
        dest.Verschijningsvorm = latestVersion.Verschijningsvorm;
        dest.Trefwoorden = latestVersion.Trefwoorden;
        dest.InhoudIsVervallen = latestVersion.InhoudIsVervallen;

        dest.BestandsDelen = latestVersion.BestandsDelen.OrderBy(d => d.Volgnummer).Select(MapBestandsDeel).ToList();
    }

    private Documenten.Contracts.v1._1.Responses.BestandsDeelResponseDto MapBestandsDeel(BestandsDeel bestandsdeel)
    {
        var uri = _uriService.GetUri(bestandsdeel);

        return new Documenten.Contracts.v1._1.Responses.BestandsDeelResponseDto
        {
            Url = uri,
            Omvang = bestandsdeel.Omvang,
            Volgnummer = bestandsdeel.Volgnummer,
            Voltooid = bestandsdeel.Voltooid,
            Lock = bestandsdeel.EnkelvoudigInformatieObjectVersie.InformatieObject.Lock,
        };
    }
}
