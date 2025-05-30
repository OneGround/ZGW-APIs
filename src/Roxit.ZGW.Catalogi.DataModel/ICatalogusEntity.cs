using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

public interface ICatalogusEntity : IUrlEntity
{
    Catalogus Catalogus { get; }
}
