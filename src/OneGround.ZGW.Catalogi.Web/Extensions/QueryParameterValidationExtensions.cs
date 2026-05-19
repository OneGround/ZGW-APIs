using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Catalogi.Web.Extensions;

public static class QueryParameterValidationExtensions
{
    public static void AddQueryParameterValidations(this IServiceCollection services)
    {
        // Register the query parameter validation filters globally for v1.0 .. v1.x
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllCatalogussenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllInformatieObjectTypenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._2.Queries.GetAllInformatieObjectTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllBesluitTypenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllBesluitTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllEigenschappenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllEigenschappenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllResultaatTypenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllResultaatTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllRolTypenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllRolTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllStatusTypenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllStatusTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllZaakObjectTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllZaakTypenQueryParameters>>();

        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1.Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters>>();
        services.AddScoped<ValidateQueryParametersFilter<Catalogi.Contracts.v1._3.Queries.GetAllZaakTypeInformatieObjectTypenQueryParameters>>();
    }
}
