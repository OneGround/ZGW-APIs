using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Documenten.Web.Extensions;

public static class QueryParameterValidationExtensions
{
    public static void AddQueryParameterValidations(this IServiceCollection services)
    {
        // Register the query parameter validation filters globally for v1.0 .. v1.x
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetAllEnkelvoudigInformatieObjectenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._5.Queries.GetAllEnkelvoudigInformatieObjectenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetAllGebruiksRechtenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._5.Queries.GetAllGebruiksRechtenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetAllObjectInformatieObjectenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._5.Queries.GetAllVerzendingenQueryParameters>>();
    }
}
