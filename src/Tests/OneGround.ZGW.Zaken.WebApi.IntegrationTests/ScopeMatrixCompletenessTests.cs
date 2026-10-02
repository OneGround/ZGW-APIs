using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Versioning;
using Xunit;
using AnchorController = OneGround.ZGW.Zaken.Web.Controllers.v1._5.ZakenController;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Holds <see cref="ScopeMatrix"/> to the <c>[Scope]</c> attributes it was copied from, so a matrix cell always tests the scope an action
/// actually asks for. Needs no containers: it reads attribute metadata only.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ScopeMatrixCompletenessTests
{
    [Fact]
    public void Every_scoped_action_has_exactly_one_matrix_row()
    {
        var actions = ScopedActions();

        // Guards a vacuous pass: reflection finding no scoped actions would leave both lists empty too.
        Assert.NotEmpty(actions);

        var duplicates = ScopeMatrix.Rows.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        var withoutRow = actions.Keys.Except(ScopeMatrix.Rows.Select(r => r.Name)).Order(StringComparer.Ordinal).ToList();
        var withoutAction = ScopeMatrix.Rows.Select(r => r.Name).Except(actions.Keys).Order(StringComparer.Ordinal).ToList();

        Assert.True(duplicates.Count == 0, $"{duplicates.Count} action(s) have more than one matrix row: {string.Join(", ", duplicates)}.");
        Assert.True(withoutRow.Count == 0, $"{withoutRow.Count} [Scope]-carrying action(s) have no matrix row: {string.Join(", ", withoutRow)}.");
        Assert.True(
            withoutAction.Count == 0,
            $"{withoutAction.Count} matrix row(s) name no [Scope]-carrying action: {string.Join(", ", withoutAction)}."
        );
    }

    [Fact]
    public void Every_matrix_row_names_the_scopes_route_and_api_version_of_its_action()
    {
        var actions = ScopedActions();
        var mismatches = new List<string>();

        foreach (var row in ScopeMatrix.Rows.Where(r => actions.ContainsKey(r.Name)))
        {
            var (controller, action) = actions[row.Name];

            var scopes = ScopesOf(action);
            if (!scopes.ToHashSet().SetEquals(row.Scopes) || scopes.Length != row.Scopes.Length)
            {
                mismatches.Add($"{row.Name}: the row has scopes [{string.Join(", ", row.Scopes)}], the attribute [{string.Join(", ", scopes)}]");
            }

            var routes = action.GetCustomAttributes<HttpMethodAttribute>().ToList();
            if (!routes.Any(r => r.Template == row.Route && r.HttpMethods.Contains(row.Method.Method)))
            {
                mismatches.Add(
                    $"{row.Name}: the row has {row.Method} {row.Route}, the action "
                        + string.Join(" and ", routes.Select(r => $"{string.Join("/", r.HttpMethods)} {r.Template}"))
                );
            }

            var versions = ApiVersionsOf(controller, action);
            if (!versions.Contains(row.ApiVersion))
            {
                mismatches.Add($"{row.Name}: the row has API version {row.ApiVersion}, the action serves [{string.Join(", ", versions)}]");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            $"{mismatches.Count} matrix row(s) differ from their action:{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}"
        );
    }

    private static Dictionary<string, (Type Controller, MethodInfo Action)> ScopedActions()
    {
        const string ControllersNamespace = "OneGround.ZGW.Zaken.Web.Controllers.";

        return typeof(AnchorController)
            .Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes<BaseScopeAttribute>(inherit: true).Any())
                    .Select(m => (Controller: t, Action: m))
            )
            .ToDictionary(a => $"{a.Controller.FullName![ControllersNamespace.Length..]}.{a.Action.Name}");
    }

    // The scopes are a constructor argument the attribute keeps private, so they are read from its metadata.
    private static string[] ScopesOf(MethodInfo action)
    {
        var attribute = Assert.Single(action.GetCustomAttributesData(), a => typeof(BaseScopeAttribute).IsAssignableFrom(a.AttributeType));
        var scopes = (ReadOnlyCollection<CustomAttributeTypedArgument>)Assert.Single(attribute.ConstructorArguments).Value!;

        return scopes.Select(s => (string)s.Value).ToArray();
    }

    // A controller declares the versions it serves either on the class or on each of its actions.
    private static string[] ApiVersionsOf(Type controller, MethodInfo action)
    {
        return VersionsDeclaredOn(controller).Concat(VersionsDeclaredOn(action)).ToArray();
    }

    private static string[] VersionsDeclaredOn(MemberInfo member)
    {
        return member
            .GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(ZgwApiVersionAttribute))
            .Select(a => (string)a.ConstructorArguments[0].Value)
            .ToArray();
    }
}
