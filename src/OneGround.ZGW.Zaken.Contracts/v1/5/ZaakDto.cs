using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace OneGround.ZGW.Zaken.Contracts.v1._5;

public abstract class ZaakDto
{
    [JsonProperty("identificatie", Order = 3)]
    public string Identificatie { get; set; }

    [JsonProperty("bronorganisatie", Order = 4)]
    public string Bronorganisatie { get; set; }

    [JsonProperty("omschrijving", Order = 5)]
    public string Omschrijving { get; set; }

    [JsonProperty("toelichting", Order = 6)]
    public string Toelichting { get; set; }

    [JsonProperty("zaaktype", Order = 7)]
    public string Zaaktype { get; set; }

    [JsonProperty("registratiedatum", Order = 8)]
    public string Registratiedatum { get; set; }

    [JsonProperty("verantwoordelijkeOrganisatie", Order = 9)]
    public string VerantwoordelijkeOrganisatie { get; set; }

    [JsonProperty("startdatum", Order = 10)]
    public string Startdatum { get; set; }

    [JsonProperty("einddatumGepland", Order = 12)]
    public string EinddatumGepland { get; set; }

    [JsonProperty("uiterlijkeEinddatumAfdoening", Order = 13)]
    public string UiterlijkeEinddatumAfdoening { get; set; }

    [JsonProperty("publicatiedatum", Order = 14)]
    public string Publicatiedatum { get; set; }

    [JsonProperty("communicatiekanaal", Order = 15)]
    public string Communicatiekanaal { get; set; } = ""; // Note: If the field is not specified (i.e. null) it must behave as an empty string. This will allow validation to work if this field is not specified

    [JsonProperty("productenOfDiensten", Order = 16)]
    public List<string> ProductenOfDiensten { get; set; } = [];

    [JsonProperty("vertrouwelijkheidaanduiding", Order = 17)]
    public string Vertrouwelijkheidaanduiding { get; set; }

    [JsonProperty("betalingsindicatie", Order = 18)]
    public string Betalingsindicatie { get; set; }

    [JsonProperty("laatsteBetaaldatum", Order = 20)]
    public string LaatsteBetaaldatum { get; set; }

    [JsonProperty("zaakgeometrie", Order = 21)]
    public Geometry Zaakgeometrie { get; set; }

    [JsonProperty("verlenging", Order = 22)]
    public ZaakVerlengingDto Verlenging { get; set; }

    [JsonProperty("opschorting", Order = 23)]
    public ZaakOpschortingDto Opschorting { get; set; }

    [JsonProperty("selectielijstklasse", Order = 24)]
    public string Selectielijstklasse { get; set; } = ""; // Note: If the field is not specified (i.e. null) it must behave as an empty string. This will allow validation to work if this field is not specified

    [JsonProperty("hoofdzaak", Order = 25)]
    public string Hoofdzaak { get; set; }

    [JsonProperty("relevanteAndereZaken", Order = 27)]
    public List<RelevanteAndereZaakDto> RelevanteAndereZaken { get; set; } = [];

    [JsonProperty("kenmerken", Order = 50)]
    public List<ZaakKenmerkDto> Kenmerken { get; set; } = [];

    [JsonProperty("archiefnominatie", Order = 51)]
    public string Archiefnominatie { get; set; }

    [JsonProperty("archiefstatus", Order = 52)]
    public string Archiefstatus { get; set; }

    [JsonProperty("archiefactiedatum", Order = 53)]
    public string Archiefactiedatum { get; set; }

    [JsonProperty("opdrachtgevendeOrganisatie", Order = 54)]
    public string OpdrachtgevendeOrganisatie { get; set; }

    [JsonProperty("processobjectaard", Order = 55)]
    public string Processobjectaard { get; set; }

    [JsonProperty("startdatumBewaartermijn", Order = 56)]
    public string StartdatumBewaartermijn { get; set; }

    [JsonProperty("processobject", Order = 57)]
    public ZaakProcessobjectDto Processobject { get; set; }
}
