using System.Reflection;
using AutoMapper.Internal;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
