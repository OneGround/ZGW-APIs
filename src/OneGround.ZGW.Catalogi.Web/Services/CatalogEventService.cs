using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Services;

public class CatalogEventService : ICatalogEventService
{
    public Task OnCatalogCreatedAsync(Catalogus catalogus, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task OnCatalogDeletedAsync(Catalogus catalogus, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task OnCatalogUpdatedAsync(string oldDomein, Catalogus catalogus, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
