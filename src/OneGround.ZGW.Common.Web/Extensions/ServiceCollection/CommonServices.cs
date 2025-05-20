using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection;

public static class CommonServicesServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection services)
    {
        services.AddScoped<IServerCertificateValidator, ByPassServerCertificateValidator>();
        services.AddScoped<IPaginationHelper, PaginationHelper>();
        services.AddScoped<IPaginationUriService, PaginationUriService>();

        services.AddTransient<IValidatorInterceptor, ValidatorInterceptor>();

        // IEntityUriService must be singleton, because it is used in Automapper
        services.AddSingleton<IEntityUriService, UriService>();

        services.AddHttpContextAccessor();
        services.AddSingleton<IRequestMerger, RequestMerger>();
        services.AddSingleton<IValidatorService, ValidatorService>();
        services.AddSingleton<IErrorResponseBuilder, ErrorResponseBuilder>();
    }
}
