using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.DataModel.Authorization;

namespace OneGround.ZGW.Documenten.Web.Services;

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
