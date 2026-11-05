using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Gesprekken.Contracts.v1.Queries;

namespace OneGround.ZGW.Gesprekken.Web.Extensions;

public static class QueryParameterValidationExtensions
{
    public static void AddQueryParameterValidations(this IServiceCollection services)
    {
        services.AddScoped<ValidateQueryParametersFilter<GetAllGesprekkenQueryParameters>>();
    }
}
