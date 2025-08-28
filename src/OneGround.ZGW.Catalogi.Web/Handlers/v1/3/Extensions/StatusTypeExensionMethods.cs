using System;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3.Extensions;

public static class StatusTypeExensionMethods
{
    public static bool CanBeUpdated(this StatusType statustype, StatusType other)
    {
        // Note: Not on all fields will be validated because other rules are relevant
        return statustype.BeginGeldigheid == other.BeginGeldigheid
            && // Note: For field EindeGeldigheid it is allowed to change
            statustype.BeginObject.GetValueOrDefault(DateOnly.MinValue) == other.BeginObject.GetValueOrDefault(DateOnly.MinValue)
            && statustype.EindeObject.GetValueOrDefault(DateOnly.MinValue) == other.EindeObject.GetValueOrDefault(DateOnly.MinValue)
            && statustype.Omschrijving == other.Omschrijving
            && statustype.OmschrijvingGeneriek == other.OmschrijvingGeneriek
            && statustype.VolgNummer == other.VolgNummer
            && statustype.IsEindStatus == other.IsEindStatus
            && statustype.Informeren == other.Informeren
            && statustype.Doorlooptijd == other.Doorlooptijd
            && statustype.StatusTekst == other.StatusTekst
            && statustype.Toelichting == other.Toelichting;
    }
}
