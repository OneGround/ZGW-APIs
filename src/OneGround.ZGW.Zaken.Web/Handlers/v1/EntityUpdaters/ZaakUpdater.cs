using System.Linq;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1.EntityUpdaters;

public class ZaakUpdater : IEntityUpdater<Zaak>
{
    public void Update(Zaak request, Zaak source, decimal version = 1)
    {
        source.Bronorganisatie = request.Bronorganisatie;
        source.Omschrijving = request.Omschrijving;
        source.Toelichting = request.Toelichting;
        source.Zaaktype = request.Zaaktype;
        source.Registratiedatum = request.Registratiedatum;
        source.VerantwoordelijkeOrganisatie = request.VerantwoordelijkeOrganisatie;
        source.Startdatum = request.Startdatum;
        source.EinddatumGepland = request.EinddatumGepland;
        source.UiterlijkeEinddatumAfdoening = request.UiterlijkeEinddatumAfdoening;
        source.Publicatiedatum = request.Publicatiedatum;
        source.Communicatiekanaal = request.Communicatiekanaal;
        source.ProductenOfDiensten = request.ProductenOfDiensten;
        source.VertrouwelijkheidAanduiding = request.VertrouwelijkheidAanduiding;
        source.BetalingsIndicatie = request.BetalingsIndicatie;
        source.LaatsteBetaaldatum = request.LaatsteBetaaldatum;
        source.Zaakgeometrie = request.Zaakgeometrie;
        source.Selectielijstklasse = request.Selectielijstklasse;
        source.RelevanteAndereZaken = request.RelevanteAndereZaken;
        source.RelevanteAndereZaken.ForEach(r => r.Owner = source.Owner);
        source.Archiefnominatie = request.Archiefnominatie;
        source.Archiefstatus = request.Archiefstatus;
        source.Archiefactiedatum = request.Archiefactiedatum;

        // merge kenmerken based on fields Bron and Kenmerk
        source.Kenmerken = request
            .Kenmerken.Select(kenmerk =>
            {
                return source.Kenmerken.Where(k => k.Bron == kenmerk.Bron).Where(k => k.Kenmerk == kenmerk.Kenmerk).FirstOrDefault(kenmerk);
            })
            .ToList();

        // Note: These are all readonly properties from ZaakRequest perspective so we DON'T update them!
        // -Einddatum
        // -BetalingsindicatieWeergave
        // -Deelzaken
        // -Eigenschappen
        // -Status
        // -Resultaat

        if (request.Verlenging != null)
        {
            source.Verlenging = new ZaakVerlenging { Duur = request.Verlenging.Duur, Reden = request.Verlenging.Reden };
        }
        else
        {
            source.Verlenging = null;
        }

        if (request.Opschorting != null)
        {
            source.Opschorting = new ZaakOpschorting { Indicatie = request.Opschorting.Indicatie, Reden = request.Opschorting.Reden };
        }
        else
        {
            source.Opschorting = null;
        }

        //ZRC-014
        if (request.BetalingsIndicatie == BetalingsIndicatie.nvt)
        {
            source.LaatsteBetaaldatum = null;
        }

        source.Kenmerken.ForEach(k => k.Owner = source.Owner);
        source.RelevanteAndereZaken.ForEach(r => r.Owner = source.Owner);
        if (source.Verlenging != null)
            source.Verlenging.Owner = source.Owner;

        // Note: Fields for v1.5 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.5M)
        {
            source.OpdrachtgevendeOrganisatie = request.OpdrachtgevendeOrganisatie;
            source.Processobjectaard = request.Processobjectaard;
            source.StartdatumBewaartermijn = request.StartdatumBewaartermijn;

            if (request.Processobject != null)
            {
                source.Processobject = new ZaakProcessobject
                {
                    Datumkenmerk = request.Processobject.Datumkenmerk,
                    Identificatie = request.Processobject.Identificatie,
                    Objecttype = request.Processobject.Objecttype,
                    Registratie = request.Processobject.Registratie,
                };
            }
            else
            {
                source.Processobject = null;
            }
        }
    }
}
