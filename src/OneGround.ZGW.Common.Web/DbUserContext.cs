using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web;

public class DbUserContext : IDbUserContext
{
    public DbUserContext(IHttpContextAccessor context = null)
    {
        UserId = context?.HttpContext?.GetClientId();
    }

    public string UserId { get; }
}
