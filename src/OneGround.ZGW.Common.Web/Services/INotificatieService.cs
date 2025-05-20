using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Services;

public interface INotificatieService
{
    Task NotifyAsync(Notification notification, CancellationToken cancellationToken = default);
}
