using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Services;

public interface ICatalogEventService
{
    Task OnCatalogCreatedAsync(Catalogus catalogus, CancellationToken cancellationToken);
}
