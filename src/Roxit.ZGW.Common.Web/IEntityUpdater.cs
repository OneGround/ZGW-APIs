using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Common.Web;

public interface IEntityUpdater<T>
    where T : IAuditableEntity
{
    void Update(T request, T source, decimal version = 1);
}
