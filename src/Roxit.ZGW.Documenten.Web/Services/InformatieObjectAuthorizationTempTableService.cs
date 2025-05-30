using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.DataModel.Authorization;

namespace Roxit.ZGW.Documenten.Web.Services;

public class InformatieObjectAuthorizationTempTableService : IInformatieObjectAuthorizationTempTableService
{
    private readonly ITemporaryTableProvider _temporaryTableProvider;

    public InformatieObjectAuthorizationTempTableService(ITemporaryTableProvider temporaryTableProvider)
    {
        _temporaryTableProvider = temporaryTableProvider;
    }

    public async Task InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        DrcDbContext drcDbContext,
        CancellationToken cancellationToken
    )
    {
        await CreateTempTableAsync(drcDbContext, cancellationToken);

        var informatieObjectTypeAuthorizations = authorizationContext.Authorization.Authorizations.Select(a => new TempInformatieObjectAuthorization
        {
            MaximumVertrouwelijkheidAanduiding = a.MaximumVertrouwelijkheidAanduiding.Value,
            InformatieObjectType = a.InformatieObjectType,
        });

        await drcDbContext.TempInformatieObjectAuthorization.AddRangeAsync(informatieObjectTypeAuthorizations, cancellationToken);

        await drcDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateTempTableAsync(DrcDbContext drcDbContext, CancellationToken cancellationToken)
    {
        const string sql = $"""
            CREATE TEMPORARY TABLE "{nameof(TempInformatieObjectAuthorization)}" 
            (
               "{nameof(TempInformatieObjectAuthorization.InformatieObjectType)}" text NOT NULL,
               "{nameof(TempInformatieObjectAuthorization.MaximumVertrouwelijkheidAanduiding)}" integer NOT NULL,
               PRIMARY KEY ("{nameof(TempInformatieObjectAuthorization.InformatieObjectType)}")
            ) 
            """;
        await _temporaryTableProvider.CreateAsync(drcDbContext, sql, cancellationToken);
    }
}
