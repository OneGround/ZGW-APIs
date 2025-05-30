namespace Roxit.ZGW.Common.ServiceAgent.Authentication;

public class AuthorizedServiceAgentAuthenticationHandlerFactory
{
    private readonly IClientJwtTokenContext _tokenContextAccessor;

    public AuthorizedServiceAgentAuthenticationHandlerFactory(IClientJwtTokenContext tokenContextAccessor)
    {
        _tokenContextAccessor = tokenContextAccessor;
    }

    public AuthorizedServiceAgentAuthenticationHandler Create()
    {
        return new AuthorizedServiceAgentAuthenticationHandler(_tokenContextAccessor);
    }
}
