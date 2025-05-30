using System;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;

public static class ZaakTypeExensionMethods
{
    public static bool CanBeUpdated(this ZaakType zaaktype, ZaakType other)
    {
        // Note: Not on all fields will be validated because other rules are relevant
        return zaaktype.Identificatie == other.Identificatie
            && zaaktype.Omschrijving == other.Omschrijving
            && zaaktype.OmschrijvingGeneriek == other.OmschrijvingGeneriek
            && zaaktype.VertrouwelijkheidAanduiding == other.VertrouwelijkheidAanduiding
            && zaaktype.Doel == other.Doel
            && zaaktype.Aanleiding == other.Aanleiding
            && zaaktype.Toelichting == other.Toelichting
            && zaaktype.IndicatieInternOfExtern == other.IndicatieInternOfExtern
            && zaaktype.HandelingInitiator == other.HandelingInitiator
            && zaaktype.Onderwerp == other.Onderwerp
            && zaaktype.HandelingBehandelaar == other.HandelingBehandelaar
            && zaaktype.Doorlooptijd == other.Doorlooptijd
            && zaaktype.Servicenorm == other.Servicenorm
            && zaaktype.OpschortingEnAanhoudingMogelijk == other.OpschortingEnAanhoudingMogelijk
            && zaaktype.VerlengingMogelijk == other.VerlengingMogelijk
            && zaaktype.VerlengingsTermijn == other.VerlengingsTermijn
            && zaaktype.PublicatieIndicatie == other.PublicatieIndicatie
            && zaaktype.PublicatieTekst == other.PublicatieTekst
            && zaaktype.SelectielijstProcestype == other.SelectielijstProcestype
            && zaaktype.HandelingBehandelaar == other.HandelingBehandelaar
            && zaaktype.BeginGeldigheid == other.BeginGeldigheid
            && // Note: For field EindeGeldigheid it is allowed to change
            zaaktype.VersieDatum == other.VersieDatum
            && zaaktype.Verantwoordelijke == other.Verantwoordelijke
            && zaaktype.BeginObject.GetValueOrDefault(DateOnly.MinValue) == other.BeginObject.GetValueOrDefault(DateOnly.MinValue)
            && zaaktype.EindeObject.GetValueOrDefault(DateOnly.MinValue) == other.EindeObject.GetValueOrDefault(DateOnly.MinValue)
            && zaaktype.BronCatalogus.CanBeUpdated(other.BronCatalogus)
            && zaaktype.BronZaaktype.CanBeUpdated(other.BronZaaktype)
            && zaaktype.ReferentieProces.CanBeUpdated(other.ReferentieProces);
    }

    private static bool CanBeUpdated(this ReferentieProces referentieproces, ReferentieProces other)
    {
        if (referentieproces == null && other == null)
            return true;
        if (referentieproces == null && other != null)
            return false;
        if (referentieproces != null && other == null)
            return false;

        return referentieproces.Naam == other.Naam && referentieproces.Link == other.Link;
    }

    private static bool CanBeUpdated(this BronCatalogus broncatalogus, BronCatalogus other)
    {
        if (broncatalogus == null && other == null)
            return true;
        if (broncatalogus == null)
            return false;
        if (other == null)
            return false;

        return broncatalogus.Url == other.Url && broncatalogus.Domein == other.Domein && broncatalogus.Rsin == other.Rsin;
    }

    private static bool CanBeUpdated(this BronZaaktype bronzaaktype, BronZaaktype other)
    {
        if (bronzaaktype == null && other == null)
            return true;
        if (bronzaaktype == null)
            return false;
        if (other == null)
            return false;

        return bronzaaktype.Url == other.Url && bronzaaktype.Identificatie == other.Identificatie && bronzaaktype.Omschrijving == other.Omschrijving;
    }
}
