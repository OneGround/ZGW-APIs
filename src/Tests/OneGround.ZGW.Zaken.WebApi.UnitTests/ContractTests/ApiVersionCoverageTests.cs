using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Versioning;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ContractTests;

/// <summary>
/// 1.7 is a superset of 1.5: every operation (verb + route) that exists in 1.5 must also exist in 1.7, either on a shared action that lists
/// both versions or on a dedicated 1.7 action. Otherwise a client that sends Api-Version 1.7.0 gets a 405 for that operation.
/// </summary>
public class ApiVersionCoverageTests
{
    private sealed record ZaakAction(
        string Verb,
        string Route,
        string Name,
        HashSet<string> Versions,
        bool DeprecatedIn15,
        bool HasETag,
        bool HasExpand
    );

    private static List<ZaakAction> GetActions()
    {
        var result = new List<ZaakAction>();
        var controllers = typeof(Web.Controllers.Api).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        foreach (var controller in controllers)
        {
            var classVersions = ReadVersions(controller.GetCustomAttributesData());

            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var versions = classVersions.Concat(ReadVersions(method.GetCustomAttributesData())).ToList();

                foreach (var http in method.GetCustomAttributes<HttpMethodAttribute>())
                {
                    result.Add(
                        new ZaakAction(
                            Verb: http.HttpMethods.Single(),
                            Route: http.Template,
                            Name: $"{controller.FullName}.{method.Name}",
                            Versions: versions.Select(v => v.MajorMinor).ToHashSet(),
                            DeprecatedIn15: versions.Any(v => v.MajorMinor == "1.5" && v.Deprecated),
                            HasETag: method.GetCustomAttribute<ETagFilter>() is not null,
                            HasExpand: method.GetCustomAttribute<Expand>() is not null
                        )
                    );
                }
            }
        }

        return result;
    }

    private static IEnumerable<(string MajorMinor, bool Deprecated)> ReadVersions(IEnumerable<CustomAttributeData> attributes)
    {
        foreach (var attribute in attributes.Where(a => a.AttributeType == typeof(ZgwApiVersionAttribute)))
        {
            var version = (string)attribute.ConstructorArguments[0].Value!;
            var deprecated = attribute.NamedArguments.Any(n => n.MemberName == "Deprecated" && n.TypedValue.Value is true);

            yield return (string.Join('.', version.Split('.').Take(2)), deprecated);
        }
    }

    [Fact]
    public void EveryOperationOf15_IsAlsoAvailableIn17()
    {
        var actions = GetActions();

        var missing = actions
            .GroupBy(a => (a.Verb, a.Route))
            .Where(g => g.Any(a => a.Versions.Contains("1.5") && !a.DeprecatedIn15))
            .Where(g => !g.Any(a => a.Versions.Contains("1.7")))
            .Select(g => $"{g.Key.Verb} {g.Key.Route} ({string.Join(", ", g.Select(a => a.Name))})")
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Operations in 1.5 that are not available in 1.7:" + Environment.NewLine + string.Join(Environment.NewLine, missing)
        );
    }

    [Fact]
    public void EveryOperationWithAnETagIn15_AlsoHasOneIn17()
    {
        // Note: a dedicated 1.7 action must keep the caching contract (ETag / If-None-Match) of the 1.5 action it replaces
        var missing = GetActions()
            .GroupBy(a => (a.Verb, a.Route))
            .Where(g => g.Any(a => a.Versions.Contains("1.5") && a.HasETag))
            .SelectMany(g => g.Where(a => a.Versions.Contains("1.7") && !a.HasETag))
            .Select(a => $"{a.Verb} {a.Route} ({a.Name})")
            .ToList();

        Assert.True(
            missing.Count == 0,
            "1.7 actions without the [ETagFilter] of their 1.5 counterpart:" + Environment.NewLine + string.Join(Environment.NewLine, missing)
        );
    }

    [Fact]
    public void EveryOperationWithTheExpandMarkerIn15_AlsoHasOneIn17()
    {
        // Note: [Expand] only documents the expand query parameter in Swagger, so a missing one is easy to overlook
        var missing = GetActions()
            .GroupBy(a => (a.Verb, a.Route))
            .Where(g => g.Any(a => a.Versions.Contains("1.5") && a.HasExpand))
            .SelectMany(g => g.Where(a => a.Versions.Contains("1.7") && !a.HasExpand))
            .Select(a => $"{a.Verb} {a.Route} ({a.Name})")
            .ToList();

        Assert.True(
            missing.Count == 0,
            "1.7 actions without the [Expand] marker of their 1.5 counterpart:" + Environment.NewLine + string.Join(Environment.NewLine, missing)
        );
    }

    [Fact]
    public void EveryOperationIn17_IsServedByExactlyOneAction()
    {
        var ambiguous = GetActions()
            .Where(a => a.Versions.Contains("1.7"))
            .GroupBy(a => (a.Verb, a.Route))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Verb} {g.Key.Route} ({string.Join(", ", g.Select(a => a.Name))})")
            .ToList();

        Assert.True(
            ambiguous.Count == 0,
            "Operations with more than one 1.7 action:" + Environment.NewLine + string.Join(Environment.NewLine, ambiguous)
        );
    }
}
