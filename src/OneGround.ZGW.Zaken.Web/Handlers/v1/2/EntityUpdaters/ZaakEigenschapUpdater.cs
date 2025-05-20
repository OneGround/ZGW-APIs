using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters;

public class ZaakEigenschapUpdater : IEntityUpdater<ZaakEigenschap>
{
    public void Update(ZaakEigenschap request, ZaakEigenschap source, decimal version = 1)
    {
        source.Eigenschap = request.Eigenschap;
        source.Waarde = request.Waarde;

        source.Owner = source.Zaak.Owner;
    }
}
