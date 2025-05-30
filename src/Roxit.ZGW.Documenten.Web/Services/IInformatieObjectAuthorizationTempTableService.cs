using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.Services;

public interface IInformatieObjectAuthorizationTempTableService
{
    Task InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        DrcDbContext drcDbContext,
        CancellationToken cancellationToken
    );
}
