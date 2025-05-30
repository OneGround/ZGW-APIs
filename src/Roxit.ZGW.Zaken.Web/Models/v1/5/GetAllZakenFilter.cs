using System;
using System.Collections.Generic;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Models.v1._5;

public class GetAllZakenFilter
{
    public string Identificatie { get; set; }
    public string Bronorganisatie { get; set; }
    public string Zaaktype { get; set; }
    public ArchiefNominatie? Archiefnominatie { get; set; }
    public IList<ArchiefNominatie> Archiefnominatie__in { get; set; }
    public DateOnly? Archiefactiedatum { get; set; }
    public DateOnly? Archiefactiedatum__lt { get; set; }
    public DateOnly? Archiefactiedatum__gt { get; set; }
    public ArchiefStatus? Archiefstatus { get; set; }
    public IList<ArchiefStatus> Archiefstatus__in { get; set; }
    public DateOnly? Startdatum { get; set; }
    public DateOnly? Startdatum__gt { get; set; }
    public DateOnly? Startdatum__gte { get; set; }
    public DateOnly? Startdatum__lt { get; set; }
    public DateOnly? Startdatum__lte { get; set; }
    public IList<string> Bronorganisatie__in { get; set; }
    public bool? Archiefactiedatum__isnull { get; set; }
    public DateOnly? Registratiedatum { get; set; }
    public DateOnly? Registratiedatum__gt { get; set; }
    public DateOnly? Registratiedatum__lt { get; set; }
    public DateOnly? Einddatum { get; set; }
    public bool? Einddatum__isnull { get; set; }
    public DateOnly? Einddatum__gt { get; set; }
    public DateOnly? Einddatum__lt { get; set; }
    public DateOnly? EinddatumGepland { get; set; }
    public DateOnly? EinddatumGepland__gt { get; set; }
    public DateOnly? EinddatumGepland__lt { get; set; }
    public DateOnly? UiterlijkeEinddatumAfdoening { get; set; }
    public DateOnly? UiterlijkeEinddatumAfdoening__gt { get; set; }
    public DateOnly? UiterlijkeEinddatumAfdoening__lt { get; set; }
    public BetrokkeneType? Rol__betrokkeneType { get; set; }
    public string Rol__betrokkene { get; set; }
    public OmschrijvingGeneriek? Rol__omschrijvingGeneriek { get; set; }
    public VertrouwelijkheidAanduiding? MaximaleVertrouwelijkheidaanduiding { get; set; }
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn { get; set; }
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie { get; set; }
    public string Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer { get; set; }
    public string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId { get; set; }
    public string Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie { get; set; }
    public string Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer { get; set; }
    public string Rol__betrokkeneIdentificatie__medewerker__identificatie { get; set; }
    public string Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie { get; set; }
}
