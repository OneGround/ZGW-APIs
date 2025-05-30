using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Roxit.ZGW.DataAccess;

public interface IDatabaseSeeder<TDbContext>
    where TDbContext : DbContext
{
    Task SeedDataAsync();
}
