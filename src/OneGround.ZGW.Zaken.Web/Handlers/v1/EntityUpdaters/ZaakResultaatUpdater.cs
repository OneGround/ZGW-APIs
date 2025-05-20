using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1.EntityUpdaters;

public class ZaakResultaatUpdater : IEntityUpdater<ZaakResultaat>
{
    public void Update(ZaakResultaat request, ZaakResultaat source, decimal version = 1)
    {
        source.Toelichting = request.Toelichting;
        source.Owner = source.Zaak.Owner;
    }
}
