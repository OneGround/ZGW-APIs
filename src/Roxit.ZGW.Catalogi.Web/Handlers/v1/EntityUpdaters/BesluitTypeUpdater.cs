using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class BesluitTypeUpdater : IEntityUpdater<BesluitType>
{
    public void Update(BesluitType request, BesluitType source, decimal version = 1)
    {
        source.Omschrijving = request.Omschrijving;
        source.OmschrijvingGeneriek = request.OmschrijvingGeneriek;
        source.BesluitCategorie = request.BesluitCategorie;
        source.ReactieTermijn = request.ReactieTermijn;
        source.PublicatieIndicatie = request.PublicatieIndicatie;
        source.PublicatieTekst = request.PublicatieTekst;
        source.PublicatieTermijn = request.PublicatieTermijn;
        source.Toelichting = request.Toelichting;
        source.BeginGeldigheid = request.BeginGeldigheid;
        source.EindeGeldigheid = request.EindeGeldigheid;

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            source.BeginObject = request.BeginObject;
            source.EindeObject = request.EindeObject;
        }
    }
}
