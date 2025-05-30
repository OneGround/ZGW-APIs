using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Services;

public class CatalogEventService : ICatalogEventService
{
    public Task OnCatalogCreatedAsync(Catalogus catalogus, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
