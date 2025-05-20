using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OneGround.ZGW.DataAccess;

public interface IDatabaseInitializer<TDbContext>
    where TDbContext : DbContext
{
    Task InitializeAsync();
}
