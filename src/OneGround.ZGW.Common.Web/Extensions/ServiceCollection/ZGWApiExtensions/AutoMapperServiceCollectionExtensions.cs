using System.Reflection;
using AutoMapper.Internal;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Mapping;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;

public static class AutoMapperServiceCollectionExtensions
{
    public static IServiceCollection AddAutoMapper(this IServiceCollection services, Assembly callingAssembly)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        services.AddAutoMapper(
            mappingConfiguration =>
            {
                mappingConfiguration.ShouldMapMethod = m => false;
                mappingConfiguration.Internal().Mappers.Insert(0, new NullableEnumMapper());
            },
            callingAssembly,
            executingAssembly
        );

        // Default IZgwMapper for every service. AddZgwMapster replaces this when a service enables
        // Mapster — which is why AddZGWApi must keep calling AddAutoMapper BEFORE AddZgwMapster.
        services.AddScoped<IZgwMapper, AutoMapperZgwMapper>();

        return services;
    }
}
