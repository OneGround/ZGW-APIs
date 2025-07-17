using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.Authorization;

namespace OneGround.ZGW.Zaken.Web.Services;

public class ZaakAuthorizationTempTableService : IZaakAuthorizationTempTableService
{
    private readonly ITemporaryTableProvider _temporaryTableProvider;

    public ZaakAuthorizationTempTableService(ITemporaryTableProvider temporaryTableProvider)
    {
        _temporaryTableProvider = temporaryTableProvider;
    }

    public async Task InsertIZaakAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        ZrcDbContext zrcDbContext,
        CancellationToken cancellationToken
    )
    {
        await CreateTempTableAsync(zrcDbContext, cancellationToken);

        var informatieObjectTypeAuthorizations = authorizationContext
            .Authorization.Authorizations.Where(permission => permission.ZaakType != null && permission.MaximumVertrouwelijkheidAanduiding.HasValue)
            .GroupBy(permission => permission.ZaakType)
            .Select(g => new TempZaakAuthorization
            {
                MaximumVertrouwelijkheidAanduiding = g.Max(a => a.MaximumVertrouwelijkheidAanduiding!.Value),
                ZaakType = g.Key,
            });

        await zrcDbContext.TempZaakAuthorization.AddRangeAsync(informatieObjectTypeAuthorizations, cancellationToken);

        await zrcDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateTempTableAsync(ZrcDbContext zrcDbContext, CancellationToken cancellationToken)
    {
        const string sql = $"""
            DROP TABLE IF EXISTS "{nameof(TempZaakAuthorization)}";
            CREATE TEMPORARY TABLE "{nameof(TempZaakAuthorization)}"
            (
               "{nameof(TempZaakAuthorization.ZaakType)}" text NOT NULL,
               "{nameof(TempZaakAuthorization.MaximumVertrouwelijkheidAanduiding)}" integer NOT NULL,
               PRIMARY KEY ("{nameof(TempZaakAuthorization.ZaakType)}")
            );
            """;
        await _temporaryTableProvider.CreateAsync(zrcDbContext, sql, cancellationToken);
    }
}
