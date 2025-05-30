using Microsoft.AspNetCore.Mvc;
using Roxit.ZGW.Common.Contracts;

namespace Roxit.ZGW.Zaken.Contracts.v1.Queries;

public class GetAllZaakRollenQueryParameters : QueryParameters
{
    /// <summary>
    /// URL-referentie naar de ZAAK.
    /// </summary>
    [FromQuery(Name = "zaak")]
    public string Zaak { get; set; }

    /// <summary>
    /// URL-referentie naar een betrokkene gerelateerd aan de ZAAK.
    /// </summary>
    [FromQuery(Name = "betrokkene")]
    public string Betrokkene { get; set; }

    /// <summary>
    /// UType van de betrokkene.
    /// </summary>
    [FromQuery(Name = "betrokkeneType")]
    public string BetrokkeneType { get; set; }

    /// <summary>
    /// Het burgerservicenummer, bedoeld in artikel 1.1 van de Wet algemene bepalingen burgerservicenummer.
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__natuurlijkPersoon__inpBsn")]
    public string BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn { get; set; }

    /// <summary>
    /// Het door de gemeente uitgegeven unieke nummer voor een ANDER NATUURLIJK PERSOON
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie")]
    public string BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie { get; set; }

    /// <summary>
    /// Het administratienummer van de persoon, bedoeld in de Wet BRP
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer")]
    public string BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer { get; set; }

    /// <summary>
    /// Het door een kamer toegekend uniek nummer voor de INGESCHREVEN NIET-NATUURLIJK PERSOON
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId")]
    public string BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId { get; set; }

    /// <summary>
    /// Het door de gemeente uitgegeven unieke nummer voor een ANDER NIET-NATUURLIJK PERSOON
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie")]
    public string BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie { get; set; }

    /// <summary>
    /// Een korte unieke aanduiding van de Vestiging.
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__vestiging__vestigingsNummer")]
    public string BetrokkeneIdentificatie__vestiging__vestigingsNummer { get; set; }

    /// <summary>
    /// Een korte identificatie van de organisatorische eenheid.
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__organisatorischeEenheid__identificatie")]
    public string BetrokkeneIdentificatie__organisatorischeEenheid__identificatie { get; set; }

    /// <summary>
    /// Een korte unieke aanduiding van de MEDEWERKER.
    /// </summary>
    [FromQuery(Name = "betrokkeneIdentificatie__medewerker__identificatie")]
    public string BetrokkeneIdentificatie__medewerker__identificatie { get; set; }

    /// <summary>
    /// URL-referentie naar een roltype binnen het ZAAKTYPE van de ZAAK.
    /// </summary>
    [FromQuery(Name = "roltype")]
    public string RolType { get; set; }

    /// <summary>
    /// Omschrijving van de aard van de ROL, afgeleid uit het ROLTYPE.
    /// </summary>
    [FromQuery(Name = "omschrijving")]
    public string Omschrijving { get; set; }

    /// <summary>
    /// Algemeen gehanteerde benaming van de aard van de ROL, afgeleid uit het ROLTYPE.
    /// </summary>
    [FromQuery(Name = "omschrijvingGeneriek")]
    public string OmschrijvingGeneriek { get; set; }
}
