using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Common.Web.Authorization;

namespace Roxit.ZGW.Autorisaties.Web.Services;

public class DbContextAuthorizationResolver : IAuthorizationResolver
{
    private readonly AcDbContext _context;

    public DbContextAuthorizationResolver(AcDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorizedApplication> ResolveAsync(string clientId, string component, string[] scopes)
    {
        var applicatie = await _context
            .Applicaties.AsNoTracking()
            .Include(a => a.Autorisaties)
            .Where(a => a.ClientIds.Any(client => client.ClientId.ToLower() == clientId.ToLower()))
            .FirstOrDefaultAsync();

        if (applicatie == null)
            return null;

        return new AuthorizedApplication
        {
            Label = applicatie.Label,
            HasAllAuthorizations = applicatie.HeeftAlleAutorisaties,
            Authorizations = applicatie
                .Autorisaties.Where(a => a.Component.ToString().Equals(component, StringComparison.OrdinalIgnoreCase))
                .Where(a => a.Scopes.Any(s => scopes.Contains(s)))
                .Select(a => new AuthorizationPermission
                {
                    MaximumVertrouwelijkheidAanduiding = (int?)a.MaxVertrouwelijkheidaanduiding,
                    InformatieObjectType = a.InformatieObjectType,
                    ZaakType = a.ZaakType,
                    BesluitType = a.BesluitType,
                    Scopes = a.Scopes,
                })
                .ToArray(),
        };
    }
}
