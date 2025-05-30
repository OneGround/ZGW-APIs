using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters;

public class ZaakEigenschapUpdater : IEntityUpdater<ZaakEigenschap>
{
    public void Update(ZaakEigenschap request, ZaakEigenschap source, decimal version = 1)
    {
        source.Eigenschap = request.Eigenschap;
        source.Waarde = request.Waarde;

        source.Owner = source.Zaak.Owner;
    }
}
