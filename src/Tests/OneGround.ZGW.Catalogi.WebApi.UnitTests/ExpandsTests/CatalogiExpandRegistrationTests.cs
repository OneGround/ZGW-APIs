using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The expand paths that each v1.4 response offers. A resolver that is not registered would only show as an "invalid expand" at request time.
/// </summary>
public class CatalogiExpandRegistrationTests
{
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IEntityUriService>());
        services.AddCatalogiAPIExpands();
        return services.BuildServiceProvider();
    }

    private static string[] PathsOf<TResponse>(ServiceProvider provider) =>
        provider.GetServices<IExpandResolver<TResponse>>().Select(r => r.Path).Order().ToArray();

    [Fact]
    public void EveryResolverThatExpandsTypes_HasTheCacheOfItsResponseType()
    {
        // Note: the resolvers get their cache at request time, so a response type without a registered cache would only show as a 500 then
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IEntityUriService>());
        services.AddCatalogiAPIExpands();
        using var provider = services.BuildServiceProvider();

        var resolverDescriptors = services
            .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IExpandResolver<>))
            .ToList();

        // Note: a resolver registered with a factory has no implementation type to check, so it would silently escape this test
        Assert.All(resolverDescriptors, d => Assert.True(d.ImplementationType is not null, $"{d.ServiceType} is not registered by type"));

        var resolverTypes = resolverDescriptors.Select(d => d.ImplementationType).Distinct().ToList();

        var withoutCache = new List<string>();
        foreach (var resolverType in resolverTypes)
        {
            var baseType = resolverType;
            while (baseType is not null && !(baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Types_ResolverBase<,,>)))
            {
                baseType = baseType.BaseType;
            }

            if (baseType is null)
            {
                continue; // Note: not a resolver of the types a parent refers to (e.g. Catalogus_Resolver, which gets its cache injected)
            }

            var cacheType = typeof(IGenericCache<>).MakeGenericType(baseType.GetGenericArguments()[2]);
            if (provider.GetService(cacheType) is null)
            {
                withoutCache.Add($"{resolverType.Name} needs {cacheType}");
            }
        }

        Assert.NotEmpty(resolverTypes);
        Assert.Empty(withoutCache);
    }

    [Fact]
    public void TheCacheOfAResponseType_IsOneInstancePerRequestAndNotSharedBetweenRequests()
    {
        // Note: a cache that outlives the request (e.g. a singleton) would hand the result of one caller to another
        using var provider = BuildServices();

        using var request1 = provider.CreateScope();
        using var request2 = provider.CreateScope();
        var cacheOfRequest1 = request1.ServiceProvider.GetRequiredService<IGenericCache<ZaakTypeResponseDto>>();
        var cacheOfRequest2 = request2.ServiceProvider.GetRequiredService<IGenericCache<ZaakTypeResponseDto>>();

        Assert.Same(cacheOfRequest1, request1.ServiceProvider.GetRequiredService<IGenericCache<ZaakTypeResponseDto>>());
        Assert.NotSame(cacheOfRequest1, cacheOfRequest2);
    }

    [Fact]
    public void Catalogus_OffersTheTypesItRefersTo()
    {
        using var provider = BuildServices();

        Assert.Equal(["besluittypen", "informatieobjecttypen", "zaaktypen"], PathsOf<CatalogusResponseDto>(provider));
    }

    [Fact]
    public void ZaakType_OffersTheCatalogusAndTheTypesItRefersTo()
    {
        using var provider = BuildServices();

        Assert.Equal(
            [
                "besluittypen",
                "catalogus",
                "deelzaaktypen",
                "eigenschappen",
                "gerelateerdeZaaktypen",
                "informatieobjecttypen",
                "resultaattypen",
                "roltypen",
                "statustypen",
                "zaakobjecttypen",
            ],
            PathsOf<ZaakTypeResponseDto>(provider)
        );
    }

    [Fact]
    public void BesluitType_OffersTheCatalogusAndTheTypesItRefersTo()
    {
        using var provider = BuildServices();

        Assert.Equal(["catalogus", "informatieobjecttypen", "resultaattypen", "zaaktypen"], PathsOf<BesluitTypeResponseDto>(provider));
    }

    [Fact]
    public void Eigenschap_OffersTheCatalogusTheZaakTypeAndTheStatusType()
    {
        using var provider = BuildServices();

        Assert.Equal(["catalogus", "statustype", "zaaktype"], PathsOf<EigenschapResponseDto>(provider));
    }

    [Fact]
    public void InformatieObjectType_OffersItsCatalogus()
    {
        using var provider = BuildServices();

        Assert.Equal(["catalogus"], PathsOf<InformatieObjectTypeResponseDto>(provider));
    }
}
