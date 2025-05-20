using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Services;

public interface ICatalogEventService
{
    Task OnCatalogCreatedAsync(Catalogus catalogus, CancellationToken cancellationToken);
}
