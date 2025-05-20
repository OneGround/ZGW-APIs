using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Authorization;

public interface IAuthorizationResolver
{
    Task<AuthorizedApplication> ResolveAsync(string clientId, string component, string[] scopes);
}
