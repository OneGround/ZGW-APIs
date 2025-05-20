using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

public interface ICatalogusEntity : IUrlEntity
{
    Catalogus Catalogus { get; }
}
