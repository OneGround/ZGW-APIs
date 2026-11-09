using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Gesprekken.DataModel;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

static class GesprekLoader
{
    /// <summary>
    /// Loads a gesprek of the tenant with its deelnames (tracked), as needed for business rules and notification kenmerken.
    /// </summary>
    public static Task<Gesprek> LoadWithDeelnamesAsync(this GrcDbContext context, string rsin, Guid id, CancellationToken cancellationToken) =>
        context.Gesprekken.Include(g => g.Deelnames).Where(g => g.Owner == rsin).SingleOrDefaultAsync(g => g.Id == id, cancellationToken);
}
