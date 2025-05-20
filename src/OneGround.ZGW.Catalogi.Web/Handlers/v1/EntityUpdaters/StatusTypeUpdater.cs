using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class StatusTypeUpdater : IEntityUpdater<StatusType>
{
    public void Update(StatusType request, StatusType source, decimal version = 1)
    {
        source.Omschrijving = request.Omschrijving;
        source.OmschrijvingGeneriek = request.OmschrijvingGeneriek;
        source.StatusTekst = request.StatusTekst;
        source.VolgNummer = request.VolgNummer;
        source.Informeren = request.Informeren;

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            source.Doorlooptijd = request.Doorlooptijd;
            source.Toelichting = request.Toelichting;

            source.CheckListItemStatustypes = request.CheckListItemStatustypes;

            // Note: Derive from Zaaktype instead of getting from request (decided to do so)
            source.BeginGeldigheid = source.ZaakType.BeginGeldigheid;
            source.EindeGeldigheid = source.ZaakType.EindeGeldigheid;
            source.BeginObject = source.ZaakType.BeginObject;
            source.EindeObject = source.ZaakType.EindeObject;
            // ----
        }
    }
}
