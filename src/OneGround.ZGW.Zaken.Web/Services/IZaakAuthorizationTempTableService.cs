using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Services;

public interface IZaakAuthorizationTempTableService
{
    Task InsertIZaakAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        ZrcDbContext zrcDbContext,
        CancellationToken cancellationToken
    );
}
