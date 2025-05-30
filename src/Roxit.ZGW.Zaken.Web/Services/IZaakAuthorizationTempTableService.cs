using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Services;

public interface IZaakAuthorizationTempTableService
{
    Task InsertIZaakAuthorizationsToTempTableAsync(
        AuthorizationContext authorizationContext,
        ZrcDbContext zrcDbContext,
        CancellationToken cancellationToken
    );
}
