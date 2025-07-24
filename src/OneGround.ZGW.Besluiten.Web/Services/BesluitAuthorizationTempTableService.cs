using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.DataModel.Authorization;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Besluiten.Web.Services;

public class BesluitAuthorizationTempTableService : IBesluitAuthorizationTempTableService
{
    private readonly ITemporaryTableProvider _temporaryTableProvider;

    public BesluitAuthorizationTempTableService(ITemporaryTableProvider temporaryTableProvider)
    {
        _temporaryTableProvider = temporaryTableProvider;
    }

    public async Task InsertBesluitAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        BrcDbContext brcDbContext,
        CancellationToken cancellationToken
    )
    {
        await CreateTempTableAsync(brcDbContext, cancellationToken);

        var informatieObjectTypeAuthorizations = authorizationContext
            .Authorization.Authorizations.Where(permission => permission.BesluitType != null)
            .GroupBy(permission => permission.BesluitType)
            .Select(g => new TempBesluitAuthorization { BesluitType = g.Key });

        await brcDbContext.TempBesluitAuthorization.AddRangeAsync(informatieObjectTypeAuthorizations, cancellationToken);

        await brcDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateTempTableAsync(BrcDbContext brcDbContext, CancellationToken cancellationToken)
    {
        const string sql = $"""
            CREATE TEMPORARY TABLE "{nameof(TempBesluitAuthorization)}"
            (
               "{nameof(TempBesluitAuthorization.BesluitType)}" text NOT NULL,
               PRIMARY KEY ("{nameof(TempBesluitAuthorization.BesluitType)}")
            );
            """;
        await _temporaryTableProvider.CreateAsync(brcDbContext, sql, cancellationToken);
    }
}
