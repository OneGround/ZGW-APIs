using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public interface IDeltaBasedAuditTrailWithImporter : IAuditTrailService
{
    Task ImportAsync(AuditTrailRegel audit, CancellationToken cancellationToken = default);
}
