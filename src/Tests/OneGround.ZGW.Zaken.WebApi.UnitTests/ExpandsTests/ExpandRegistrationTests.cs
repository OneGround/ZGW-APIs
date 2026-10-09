using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The per-request caches and the zaak lookup are registered by the very method that registers the resolvers that need them, so it does not
/// matter which of these methods a host calls, or in which order (a missing registration would only show at request time).
/// </summary>
public class ExpandRegistrationTests
{
    private static readonly Dictionary<string, Action<IServiceCollection>> Methods = new()
    {
        [nameof(ExpandsServiceCollectionExtensions.AddZakenAPIExpands)] = s => s.AddZakenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddStatussenAPIExpands)] = s => s.AddStatussenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddResultatenAPIExpands)] = s => s.AddResultatenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddRollenAPIExpands)] = s => s.AddRollenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddZaakObjectenAPIExpands)] = s => s.AddZaakObjectenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddZaakContactmomentenAPIExpands)] = s => s.AddZaakContactmomentenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddZaakEigenschappenAPIExpands)] = s => s.AddZaakEigenschappenAPIExpands(),
        [nameof(ExpandsServiceCollectionExtensions.AddZaakInformatieObjectenAPIExpands)] = s => s.AddZaakInformatieObjectenAPIExpands(),
    };

    public static TheoryData<string> MethodNames => new(Methods.Keys);

    // Note: only what the per-request memoization added; the ZTC type caches are registered by one method for the others (see Startup.cs)
    private static bool IsMemoizationDependency(Type type) =>
        type == typeof(IZaakLookup)
        || (
            type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(IGenericCache<>)
            && type.GetGenericArguments()[0].IsGenericType
            && type.GetGenericArguments()[0].GetGenericTypeDefinition().Name is "QueryResult`1" or "ServiceAgentResponse`1"
        );

    [Theory]
    [MemberData(nameof(MethodNames))]
    public void EveryMemoizationDependencyOfAResolver_IsRegisteredByTheSameMethod(string method)
    {
        var services = new ServiceCollection();
        Methods[method](services);

        var missing = services
            .Where(d =>
                d.ImplementationType is not null
                && d.ServiceType.IsGenericType
                && d.ServiceType.GetGenericTypeDefinition() == typeof(IExpandResolver<>)
            )
            .SelectMany(d =>
                d.ImplementationType.GetConstructors()
                    .Single()
                    .GetParameters()
                    .Select(p => p.ParameterType)
                    .Where(IsMemoizationDependency)
                    .Where(t => !services.Any(s => s.ServiceType == t))
                    .Select(t => $"{d.ImplementationType.Name} needs {t}")
            )
            .ToList();

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void CallingTheMethodsTwice_DoesNotRegisterAnythingTwice()
    {
        var services = new ServiceCollection();
        services.AddZakenAPIExpands();
        services.AddStatussenAPIExpands();

        var duplicates = services
            .Where(d => IsMemoizationDependency(d.ServiceType) || d.ServiceType == typeof(IZaakLookup))
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.ToString())
            .ToList();

        Assert.Empty(duplicates);
    }
}
