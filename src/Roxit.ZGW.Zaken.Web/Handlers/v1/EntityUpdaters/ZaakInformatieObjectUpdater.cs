using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1.EntityUpdaters;

public class ZaakInformatieObjectUpdater : IEntityUpdater<ZaakInformatieObject>
{
    public void Update(ZaakInformatieObject request, ZaakInformatieObject source, decimal version = 1)
    {
        source.Titel = request.Titel;
        source.Beschrijving = request.Beschrijving;
        source.Owner = source.Zaak.Owner;

        // Note: Fields for v1.5 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.5M)
        {
            source.VernietigingsDatum = request.VernietigingsDatum;
        }
    }
}
