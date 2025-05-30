using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Roxit.ZGW.Zaken.Contracts.v1._5.Requests;

public class ZaakSearchRequestDto : IZakenSearchableFields
{
    [JsonProperty("zaakgeometrie")]
    public WithinGeometry ZaakGeometry { get; set; }

    [JsonProperty("identificatie")]
    public string Identificatie { get; set; }

    [JsonProperty("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [FromQuery(Name = "bronorganisatie__in")]
    public string Bronorganisatie__in { get; set; }

    [JsonProperty("zaaktype")]
    public string Zaaktype { get; set; }

    [JsonProperty("archiefnominatie")]
    public string Archiefnominatie { get; set; }

    [JsonProperty("archiefnominatie__in")]
    public string Archiefnominatie__in { get; set; }

    [JsonProperty("archiefactiedatum")]
    public string Archiefactiedatum { get; set; }

    [FromQuery(Name = "archiefactiedatum__isnull")]
    public string Archiefactiedatum__isnull { get; set; }

    [JsonProperty("archiefactiedatum__lt")]
    public string Archiefactiedatum__lt { get; set; }

    [JsonProperty("archiefactiedatum__gt")]
    public string Archiefactiedatum__gt { get; set; }

    [JsonProperty("archiefstatus")]
    public string Archiefstatus { get; set; }

    [JsonProperty("archiefstatus__in")]
    public string Archiefstatus__in { get; set; }

    [JsonProperty("startdatum")]
    public string Startdatum { get; set; }

    [JsonProperty("startdatum__gt")]
    public string Startdatum__gt { get; set; }

    [JsonProperty("startdatum__gte")]
    public string Startdatum__gte { get; set; }

    [JsonProperty("startdatum__lt")]
    public string Startdatum__lt { get; set; }

    [JsonProperty("startdatum__lte")]
    public string Startdatum__lte { get; set; }

    [JsonProperty("registratiedatum")]
    public string Registratiedatum { get; set; }

    [JsonProperty("registratiedatum__gt")]
    public string Registratiedatum__gt { get; set; }

    [JsonProperty("registratiedatum__lt")]
    public string Registratiedatum__lt { get; set; }

    [JsonProperty("einddatum")]
    public string Einddatum { get; set; }

    [JsonProperty("einddatum__isnull")]
    public string Einddatum__isnull { get; set; }

    [JsonProperty("einddatum__gt")]
    public string Einddatum__gt { get; set; }

    [JsonProperty("einddatum__lt")]
    public string Einddatum__lt { get; set; }

    [JsonProperty("einddatumGepland")]
    public string EinddatumGepland { get; set; }

    [JsonProperty("einddatumGepland__gt")]
    public string EinddatumGepland__gt { get; set; }

    [JsonProperty("einddatumGepland__lt")]
    public string EinddatumGepland__lt { get; set; }

    [JsonProperty("uiterlijkeEinddatumAfdoening")]
    public string UiterlijkeEinddatumAfdoening { get; set; }

    [JsonProperty("uiterlijkeEinddatumAfdoening__gt")]
    public string UiterlijkeEinddatumAfdoening__gt { get; set; }

    [JsonProperty("uiterlijkeEinddatumAfdoening__lt")]
    public string UiterlijkeEinddatumAfdoening__lt { get; set; }

    [JsonProperty("rol__betrokkeneType")]
    public string Rol__betrokkeneType { get; set; }

    [JsonProperty("rol__betrokkene")]
    public string Rol__betrokkene { get; set; }

    [JsonProperty("rol__omschrijvingGeneriek")]
    public string Rol__omschrijvingGeneriek { get; set; }

    [JsonProperty("maximaleVertrouwelijkheidaanduiding")]
    public string MaximaleVertrouwelijkheidaanduiding { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn")]
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie")]
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer")]
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId")]
    public string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie")]
    public string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__vestiging__vestigingsNummer")]
    public string Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__medewerker__identificatie")]
    public string Rol__betrokkeneIdentificatie__medewerker__identificatie { get; set; }

    [JsonProperty("rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie")]
    public string Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie { get; set; }
}

public class WithinGeometry
{
    [JsonProperty("within")]
    public Geometry Within { get; set; }
}
