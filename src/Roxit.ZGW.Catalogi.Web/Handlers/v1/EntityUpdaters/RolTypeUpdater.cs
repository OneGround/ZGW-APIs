using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class RolTypeUpdater : IEntityUpdater<RolType>
{
    public void Update(RolType request, RolType source, decimal version = 1)
    {
        source.Omschrijving = request.Omschrijving;
        source.OmschrijvingGeneriek = request.OmschrijvingGeneriek;

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            // Note: Derive from Zaaktype instead of getting from request (decided to do so)
            source.BeginGeldigheid = source.ZaakType.BeginGeldigheid;
            source.EindeGeldigheid = source.ZaakType.EindeGeldigheid;
            source.BeginObject = source.ZaakType.BeginObject;
            source.EindeObject = source.ZaakType.EindeObject;
            // ----
        }
    }
}
