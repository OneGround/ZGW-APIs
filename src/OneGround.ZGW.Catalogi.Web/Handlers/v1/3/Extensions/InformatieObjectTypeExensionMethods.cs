using System;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;

public static class InformatieObjectTypeExensionMethods
{
    public static bool CanBeUpdated(this InformatieObjectType informatieobjecttype, InformatieObjectType other)
    {
        // Note: Not all fields will be validated because other rules are relevant
        return informatieobjecttype.Omschrijving == other.Omschrijving
            && informatieobjecttype.VertrouwelijkheidAanduiding == other.VertrouwelijkheidAanduiding
            && informatieobjecttype.BeginGeldigheid == other.BeginGeldigheid
            && // Note: For field EindeGeldigheid it is allowed to change
            informatieobjecttype.BeginObject.GetValueOrDefault(DateOnly.MinValue) == other.BeginObject.GetValueOrDefault(DateOnly.MinValue)
            && informatieobjecttype.EindeObject.GetValueOrDefault(DateOnly.MinValue) == other.EindeObject.GetValueOrDefault(DateOnly.MinValue)
            && informatieobjecttype.InformatieObjectCategorie == other.InformatieObjectCategorie
            && informatieobjecttype.OmschrijvingGeneriek.CanBeUpdated(other.OmschrijvingGeneriek);
    }

    private static bool CanBeUpdated(this OmschrijvingGeneriek omschrijvinggeneriek, OmschrijvingGeneriek other)
    {
        if (omschrijvinggeneriek == null && other == null)
            return true;
        if (omschrijvinggeneriek == null)
            return false;
        if (other == null)
            return false;

        return omschrijvinggeneriek.InformatieObjectTypeOmschrijvingGeneriek == other.InformatieObjectTypeOmschrijvingGeneriek
            && omschrijvinggeneriek.DefinitieInformatieObjectTypeOmschrijvingGeneriek == other.DefinitieInformatieObjectTypeOmschrijvingGeneriek
            && omschrijvinggeneriek.HerkomstInformatieObjectTypeOmschrijvingGeneriek == other.HerkomstInformatieObjectTypeOmschrijvingGeneriek
            && omschrijvinggeneriek.HierarchieInformatieObjectTypeOmschrijvingGeneriek == other.HierarchieInformatieObjectTypeOmschrijvingGeneriek
            && omschrijvinggeneriek.OpmerkingInformatieObjectTypeOmschrijvingGeneriek == other.OpmerkingInformatieObjectTypeOmschrijvingGeneriek;
    }
}
