using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OneGround.ZGW.Catalogi.Web.Controllers;
using OneGround.ZGW.Common.Web.Controllers;
using Swashbuckle.AspNetCore.Annotations;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ControllerTests;

public class ControllerVersioningTests
{
    private const string ResponsesV14Namespace = "OneGround.ZGW.Catalogi.Contracts.v1._4.Responses";

    private static IEnumerable<Type> Controllers =>
        typeof(Api).Assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ZGWControllerBase).IsAssignableFrom(t));

    private sealed record Action(Type Controller, MethodInfo Method, string HttpMethod, string Template, string[] Versions);

    private static IEnumerable<Action> Actions()
    {
        foreach (var controller in Controllers)
        {
            var controllerVersions = controller
                .GetCustomAttributes<ApiVersionAttribute>()
                .SelectMany(a => a.Versions)
                .Select(v => v.ToString())
                .ToArray();

            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                // The versions of an action: its own, otherwise those of its controller
                var methodVersions = method
                    .GetCustomAttributes<ApiVersionAttribute>()
                    .SelectMany(a => a.Versions)
                    .Select(v => v.ToString())
                    .ToArray();
                var versions = methodVersions.Length > 0 ? methodVersions : controllerVersions;

                foreach (var attribute in method.GetCustomAttributes<HttpMethodAttribute>())
                {
                    foreach (var httpMethod in attribute.HttpMethods)
                    {
                        yield return new Action(controller, method, httpMethod, attribute.Template, versions);
                    }
                }
            }
        }
    }

    [Fact]
    public void Actions_Should_HaveAUniqueHttpMethodAndRoutePerVersion()
    {
        Assert.True(Actions().Count() > 100, "The actions of the controllers are not found");

        var duplicates = Actions()
            .SelectMany(a => a.Versions.Select(v => (Key: $"{a.HttpMethod} {a.Template} (v{v})", Action: a)))
            .GroupBy(x => x.Key)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => $"{x.Action.Controller.FullName}.{x.Action.Method.Name}"))}")
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void WriteActionsOfVersion14_Should_ReturnTheVersion14Response()
    {
        // The write actions have to return the same response as the read operations of the same version
        var writeActions = Actions().Where(a => a.Versions.Contains(Api.LatestVersion_1_4) && a.HttpMethod is "POST" or "PUT" or "PATCH").ToList();
        Assert.True(writeActions.Count >= 30, "The write actions of version 1.4 are not found");

        var wrong = Actions()
            .Where(a => a.Versions.Contains(Api.LatestVersion_1_4) && a.HttpMethod is "POST" or "PUT" or "PATCH")
            .SelectMany(a =>
                a.Method.GetCustomAttributes<SwaggerResponseAttribute>()
                    .Where(r => r.StatusCode is 200 or 201 && r.Type != null)
                    .Select(r => (a, r.Type))
            )
            .Where(x => x.Type.Namespace != ResponsesV14Namespace)
            .Select(x => $"{x.a.Controller.FullName}.{x.a.Method.Name} returns {x.Type.FullName}")
            .Distinct()
            .ToList();

        Assert.Empty(wrong);
    }
}
