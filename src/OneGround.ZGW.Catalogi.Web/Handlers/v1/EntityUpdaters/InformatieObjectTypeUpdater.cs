using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class InformatieObjectTypeUpdater : IEntityUpdater<InformatieObjectType>
{
    public void Update(InformatieObjectType request, InformatieObjectType source, decimal version = 1)
    {
        source.BeginGeldigheid = request.BeginGeldigheid;
        source.EindeGeldigheid = request.EindeGeldigheid;
        source.Omschrijving = request.Omschrijving;
        source.VertrouwelijkheidAanduiding = request.VertrouwelijkheidAanduiding;

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            source.BeginObject = request.BeginObject;
            source.EindeObject = request.EindeObject;
            source.InformatieObjectCategorie = request.InformatieObjectCategorie;
            source.Trefwoord = request.Trefwoord;
            source.OmschrijvingGeneriek = request.OmschrijvingGeneriek;
        }
    }
}
