using System.Threading.Tasks;

namespace Roxit.ZGW.Common.Web.Authorization;

public interface IAuthorizationResolver
{
    Task<AuthorizedApplication> ResolveAsync(string clientId, string component, string[] scopes);
}
