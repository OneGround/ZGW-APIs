using System;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

public static class ZaakObjectTypeExensionMethods
{
    public static bool CanBeUpdated(this ZaakObjectType zaakobjecttype, ZaakObjectType other)
    {
        // Note: Not on all fields will be validated because other rules are relevant
        return zaakobjecttype.BeginGeldigheid == other.BeginGeldigheid
            && // Note: For field EindeGeldigheid it is allowed to change
            zaakobjecttype.BeginObject.GetValueOrDefault(DateOnly.MinValue) == other.BeginObject.GetValueOrDefault(DateOnly.MinValue)
            && zaakobjecttype.EindeObject.GetValueOrDefault(DateOnly.MinValue) == other.EindeObject.GetValueOrDefault(DateOnly.MinValue)
            && zaakobjecttype.AnderObjectType == other.AnderObjectType
            && zaakobjecttype.ObjectType == other.ObjectType
            && zaakobjecttype.RelatieOmschrijving == other.RelatieOmschrijving;
    }
}
