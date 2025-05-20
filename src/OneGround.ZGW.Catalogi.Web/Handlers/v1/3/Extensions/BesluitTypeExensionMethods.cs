using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;

public static class BesluitTypeExensionMethods
{
    public static bool CanBeUpdated(this BesluitType zaaktype, BesluitType other)
    {
        // Note: Not on all fields will be validated because other rules are relevant
        return zaaktype.Omschrijving == other.Omschrijving
            && zaaktype.OmschrijvingGeneriek == other.OmschrijvingGeneriek
            && zaaktype.BesluitCategorie == other.BesluitCategorie
            && zaaktype.ReactieTermijn == other.ReactieTermijn
            && zaaktype.PublicatieIndicatie == other.PublicatieIndicatie
            && zaaktype.PublicatieTekst == other.PublicatieTekst
            && zaaktype.PublicatieTermijn == other.PublicatieTermijn
            && zaaktype.Toelichting == other.Toelichting
            && zaaktype.BeginGeldigheid == other.BeginGeldigheid
            && // Note: For field EindeGeldigheid it is allowed to change
            zaaktype.BeginObject == other.BeginObject
            && zaaktype.EindeObject == other.EindeObject;
    }
}
