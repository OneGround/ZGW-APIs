using Microsoft.OpenApi.Models;

namespace Roxit.ZGW.Common.Web.Swagger;

public static class SwaggerSecurityDefinitions
{
    public static class JWTBearerToken
    {
        public const string Name = "JWTBearerToken";

        public static readonly OpenApiSecurityScheme Scheme = new()
        {
            Description = "JWT Authorization header using the bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
        };

        public static readonly OpenApiReference Reference = new() { Id = Name, Type = ReferenceType.SecurityScheme };
    }
}
