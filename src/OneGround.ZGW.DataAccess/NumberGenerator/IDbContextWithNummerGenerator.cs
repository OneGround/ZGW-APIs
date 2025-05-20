using Microsoft.EntityFrameworkCore;

namespace OneGround.ZGW.DataAccess.NumberGenerator;

public interface IDbContextWithNummerGenerator
{
    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }
}
