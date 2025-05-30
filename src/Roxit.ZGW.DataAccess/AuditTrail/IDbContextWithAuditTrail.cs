using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Roxit.ZGW.DataAccess.AuditTrail;

public interface IDbContextWithAuditTrail
{
    DbSet<AuditTrailRegel> AuditTrailRegels { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
