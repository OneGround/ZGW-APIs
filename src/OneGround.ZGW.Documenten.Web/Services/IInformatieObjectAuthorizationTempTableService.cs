using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Services;

public interface IInformatieObjectAuthorizationTempTableService
{
    Task InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        DrcDbContext drcDbContext,
        CancellationToken cancellationToken
    );
}
