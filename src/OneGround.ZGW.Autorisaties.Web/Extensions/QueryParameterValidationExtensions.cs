using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Autorisaties.Web.Extensions;

public static class QueryParameterValidationExtensions
{
    public static void AddQueryParameterValidations(this IServiceCollection services)
    {
        // Register the query parameter validation filters globally for v1.0 .. v1.x
        services.AddScoped<ValidateQueryParametersFilter<Contracts.v1.Requests.Queries.GetAllApplicatiesQueryParameters>>();
    }
}
