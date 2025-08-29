using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OneGround.ZGW.Common.Web.Authentication;

public class ZgwAuthenticationConstants
{
    public const string ZgwTokenIntrospectionAuthenticationScheme = "OAuth2Introspection";
    public const string OAuth2AuthenticationScheme = JwtBearerDefaults.AuthenticationScheme;
    public const string PolicySelectorAuthenticationScheme = "ZgwAuthenticationSchemeSelector";
}
