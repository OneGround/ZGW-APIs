using System;
using System.Linq;
using AutoMapper;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.MappingProfiles.v1;

public class MapLatestEnkelvoudigInformatieObjectVersieRequest
    : IMappingAction<EnkelvoudigInformatieObject, EnkelvoudigInformatieObjectUpdateRequestDto>
{
    public void Process(EnkelvoudigInformatieObject src, EnkelvoudigInformatieObjectUpdateRequestDto dest, ResolutionContext context)
    {
        // Note: For update-request-mapping we get always get the latest version
        var latestVersion =
            src.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).Last()
            ?? throw new InvalidOperationException($"EnkelvoudigInformatieObject {src.Id} does not contain any version.");
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
        dest.InformatieObjectType = latestVersion.EnkelvoudigInformatieObject.InformatieObjectType;
        dest.IndicatieGebruiksrecht = latestVersion.EnkelvoudigInformatieObject.IndicatieGebruiksrecht;
    }
}
