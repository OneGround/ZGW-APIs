using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Documenten.Web.Extensions;

public static class QueryAndSearchParameterValidationsExtensions
{
    public static void AddQueryAndSearchParameterValidations(this IServiceCollection services)
    {
        // Register the query parameter validation filters globally for v1.0 .. v1.x
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetEnkelvoudigInformatieObjectQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetAllEnkelvoudigInformatieObjectenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._5.Queries.GetAllEnkelvoudigInformatieObjectenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._7.Queries.GetAllEnkelvoudigInformatieObjectenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetAllGebruiksRechtenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._5.Queries.GetAllGebruiksRechtenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1.Queries.GetAllObjectInformatieObjectenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Documenten.Contracts.v1._5.Queries.GetAllVerzendingenQueryParameters>>();

        // Register the body parameter validation filters globally for v1.0 .. v1.x (HTTP POST /_zoek)
        services.AddScoped<ValidateBodyParametersFilter<Documenten.Contracts.v1._5.Requests.EnkelvoudigInformatieObjectSearchRequestDto>>();
        services.AddScoped<ValidateBodyParametersFilter<Documenten.Contracts.v1._7.Requests.EnkelvoudigInformatieObjectSearchRequestDto>>();
    }
}
