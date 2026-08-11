namespace OneGround.ZGW.Documenten.Contracts.v1._7;

public interface IDocumentenCommonSearchableFields
{
    /// <summary>
    /// Het RSIN van de Niet-natuurlijk persoon zijnde de organisatie die het informatieobject heeft gecreëerd of heeft ontvangen en als eerste in een samenwerkingsketen heeft vastgelegd.
    /// </summary>
    public string Bronorganisatie { get; set; }

    /// <summary>
    /// Een binnen een gegeven context ondubbelzinnige referentie naar het INFORMATIEOBJECT.
    /// </summary>
    public string Identificatie { get; set; }

    /// <summary>
    /// Een lijst van trefwoorden gescheiden door comma's. Example: trefwoorden=bouwtekening,vergunning,aanvraag
    /// </summary>
    public string Trefwoorden { get; set; }

    /// <summary>
    /// URL-referentie naar het gerelateerde OBJECT (in deze of andere API).
    /// </summary>
    public string ObjectInformatieObjecten_Object { get; set; }

    /// <summary>
    /// Enum: "besluit" "zaak" "verzoek" - Het type van het gerelateerde OBJECT.
    /// </summary>
    public string ObjectInformatieObjecten_ObjectType { get; set; }
}
