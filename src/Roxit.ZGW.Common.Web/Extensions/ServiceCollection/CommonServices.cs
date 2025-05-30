using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Services;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Common.Web.Extensions.ServiceCollection;

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
