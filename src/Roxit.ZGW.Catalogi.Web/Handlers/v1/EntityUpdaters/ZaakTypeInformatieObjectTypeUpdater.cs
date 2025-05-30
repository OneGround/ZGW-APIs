using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class ZaakTypeInformatieObjectTypeUpdater : IEntityUpdater<ZaakTypeInformatieObjectType>
{
    public void Update(ZaakTypeInformatieObjectType request, ZaakTypeInformatieObjectType source, decimal version = 1)
    {
        source.Richting = request.Richting;
        source.VolgNummer = request.VolgNummer;
    }
}
