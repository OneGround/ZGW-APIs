using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Besluiten.Web.Extensions;

public static class QueryParameterValidationExtensions
{
    public static void AddQueryParameterValidations(this IServiceCollection services)
    {
        // Register the query parameter validation filters globally for v1.0 .. v1.x
        services.AddScoped<ValidateQueryParametersFilter<Besluiten.Contracts.v1.Queries.GetAllBesluitenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Besluiten.Contracts.v1.Queries.GetAllBesluitInformatieObjectenQueryParameters>>();
    }
}
