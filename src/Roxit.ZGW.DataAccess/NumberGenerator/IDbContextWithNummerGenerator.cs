using Microsoft.EntityFrameworkCore;

namespace Roxit.ZGW.DataAccess.NumberGenerator;

public interface IDbContextWithNummerGenerator
{
    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }
}
