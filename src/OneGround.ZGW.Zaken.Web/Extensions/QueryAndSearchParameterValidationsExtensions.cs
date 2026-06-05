using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Zaken.Web.Extensions;

public static class QueryAndSearchParameterValidationsExtensions
{
    public static void AddQueryAndSearchParameterValidations(this IServiceCollection services)
    {
        // Register the query parameter validation filters globally for v1.0 .. v1.x
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllZakenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1._5.Queries.GetAllZakenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllZaakObjectenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1._5.Queries.GetAllZaakContactmomentenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllZaakInformatieObjectenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllZaakRollenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllZaakStatussenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1._5.Queries.GetAllZaakStatussenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1._5.Queries.GetAllZaakVerzoekenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllKlantContactenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Zaken.Contracts.v1.Queries.GetAllZaakResultatenQueryParameters>>();

        // Register the body parameter validation filters globally for v1.0 .. v1.x (HTTP POST /_zoek)
        services.AddScoped<ValidateBodyParametersFilter<Zaken.Contracts.v1.Requests.ZaakSearchRequestDto>>();
        services.AddScoped<ValidateBodyParametersFilter<Zaken.Contracts.v1._5.Requests.ZaakSearchRequestDto>>();
    }
}
