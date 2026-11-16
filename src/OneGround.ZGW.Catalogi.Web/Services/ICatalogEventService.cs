using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Services;

/// <summary>
/// Lets the host react on a created or updated catalogus. The methods are called after the changes are saved but before they are committed, in the
/// same database transaction: when a method throws, the database changes are rolled back and the request fails, so a retry is safe.
/// What an implementation did outside the database before it threw (e.g. in an external store) is NOT rolled back. Implementations must therefore be
/// idempotent and should do their most failure-prone step first, and the transaction is open while they run so they should be quick.
/// </summary>
public interface ICatalogEventService
{
    Task OnCatalogCreatedAsync(Catalogus catalogus, CancellationToken cancellationToken);
    Task OnCatalogUpdatedAsync(string oldDomein, Catalogus catalogus, CancellationToken cancellationToken);
}
