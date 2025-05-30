using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class ZaakObjectTypeUpdater : IEntityUpdater<ZaakObjectType>
{
    public void Update(ZaakObjectType request, ZaakObjectType source, decimal version = 1.3M)
    {
        source.AnderObjectType = request.AnderObjectType;
        source.BeginGeldigheid = request.BeginGeldigheid;
        source.EindeGeldigheid = request.EindeGeldigheid;
        source.BeginObject = request.BeginObject;
        source.EindeObject = request.EindeObject;
        source.ObjectType = request.ObjectType;
        source.RelatieOmschrijving = request.RelatieOmschrijving;

        // Note: Fields for v1.4 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.4M)
        {
            // TODO: Reserved for later version(s)
        }
    }
}
