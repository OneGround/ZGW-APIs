using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class ZaakTypeUpdater : IEntityUpdater<ZaakType>
{
    public void Update(ZaakType request, ZaakType source, decimal version = 1)
    {
        source.ReferentieProces.Link = request.ReferentieProces.Link;
        source.ReferentieProces.Naam = request.ReferentieProces.Naam;
        source.Identificatie = request.Identificatie;
        source.Omschrijving = request.Omschrijving;
        source.OmschrijvingGeneriek = request.OmschrijvingGeneriek;
        source.VertrouwelijkheidAanduiding = request.VertrouwelijkheidAanduiding;
        source.Doel = request.Doel;
        source.Aanleiding = request.Aanleiding;
        source.Toelichting = request.Toelichting;
        source.IndicatieInternOfExtern = request.IndicatieInternOfExtern;
        source.HandelingInitiator = request.HandelingInitiator;
        source.Onderwerp = request.Onderwerp;
        source.HandelingBehandelaar = request.HandelingBehandelaar;
        source.Doorlooptijd = request.Doorlooptijd;
        source.Servicenorm = request.Servicenorm;
        source.OpschortingEnAanhoudingMogelijk = request.OpschortingEnAanhoudingMogelijk;
        source.VerlengingMogelijk = request.VerlengingMogelijk;
        source.VerlengingsTermijn = request.VerlengingsTermijn;
        source.Trefwoorden = request.Trefwoorden;
        source.PublicatieIndicatie = request.PublicatieIndicatie;
        source.PublicatieTekst = request.PublicatieTekst;
        source.Verantwoordingsrelatie = request.Verantwoordingsrelatie;
        source.ProductenOfDiensten = request.ProductenOfDiensten;
        source.SelectielijstProcestype = request.SelectielijstProcestype;
        source.BeginGeldigheid = request.BeginGeldigheid;
        source.EindeGeldigheid = request.EindeGeldigheid;
        source.VersieDatum = request.VersieDatum;

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            source.Verantwoordelijke = request.Verantwoordelijke;
            source.BeginObject = request.BeginObject;
            source.EindeObject = request.EindeObject;

            source.BronCatalogus = request.BronCatalogus;
            source.BronZaaktype = request.BronZaaktype;
        }
    }
}
