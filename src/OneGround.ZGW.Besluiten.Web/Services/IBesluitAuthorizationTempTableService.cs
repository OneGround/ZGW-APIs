using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.Besluiten.Web.Services;

public interface IBesluitAuthorizationTempTableService
{
    Task InsertBesluitAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        BrcDbContext brcDbContext,
        CancellationToken cancellationToken
    );
}
