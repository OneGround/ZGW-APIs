using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.Authorization;

namespace Roxit.ZGW.Zaken.Web.Services;

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

        var informatieObjectTypeAuthorizations = authorizationContext.Authorization.Authorizations.Select(a => new TempZaakAuthorization
        {
            MaximumVertrouwelijkheidAanduiding = a.MaximumVertrouwelijkheidAanduiding.Value,
            ZaakType = a.ZaakType,
        });

        await zrcDbContext.TempZaakAuthorization.AddRangeAsync(informatieObjectTypeAuthorizations, cancellationToken);

        await zrcDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateTempTableAsync(ZrcDbContext zrcDbContext, CancellationToken cancellationToken)
    {
        const string sql = $"""
            CREATE TEMPORARY TABLE "{nameof(TempZaakAuthorization)}" 
            (
               "{nameof(TempZaakAuthorization.ZaakType)}" text NOT NULL,
               "{nameof(TempZaakAuthorization.MaximumVertrouwelijkheidAanduiding)}" integer NOT NULL,
               PRIMARY KEY ("{nameof(TempZaakAuthorization.ZaakType)}")
            ) 
            """;
        await _temporaryTableProvider.CreateAsync(zrcDbContext, sql, cancellationToken);
    }
}
