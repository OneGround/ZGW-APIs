using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web;

public interface IEntityUpdater<T>
    where T : IAuditableEntity
{
    void Update(T request, T source, decimal version = 1);
}
