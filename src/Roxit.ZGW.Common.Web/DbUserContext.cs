using Microsoft.AspNetCore.Http;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Common.Web;

public class DbUserContext : IDbUserContext
{
    public DbUserContext(IHttpContextAccessor context = null)
    {
        UserId = context?.HttpContext?.GetClientId();
    }

    public string UserId { get; }
}
