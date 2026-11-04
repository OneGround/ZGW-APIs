using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OneGround.ZGW.Common.Web.Authorization;
using Xunit;
using AnchorController = OneGround.ZGW.Zaken.Web.Controllers.v1._5.ZakenController;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Holds <see cref="ScopeMatrix"/> to the <c>[Scope]</c> attributes it was copied from, reading attribute metadata only.
/// </summary>
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
            var (_, action) = actions[row.Name];

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

            var versions = row.ServedApiVersions;
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

    [Fact]
    public void Every_matrix_row_on_one_zaak_has_zaaktype_and_vertrouwelijkheid_tests()
    {
        // Zaak list, read and create are covered by ZaakTypeAuthorizationTests and the zaak-create theories; a reservation has no zaak.
        string[] notOnOneZaak =
        [
            "ZakenController.GetAllAsync",
            "ZakenController.GetAsync",
            "ZakenController.AddAsync",
            "ZaaknummerReserverenController.ReserveAsync",
        ];

        var covered = ZaakSubResourceAuthorizationTests
            .Endpoints.Select(e => e.Row)
            .Concat(ZaakSubResourceListAuthorizationTests.FilteredLists)
            .Concat(ZaakSubResourceListAuthorizationTests.ListsOfOneZaak)
            .ToHashSet();

        var uncovered = ScopeMatrix
            .Rows.Select(r => r.Name)
            .Where(n => !covered.Contains(n) && !notOnOneZaak.Any(x => n.EndsWith("." + x, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            uncovered.Count == 0,
            $"{uncovered.Count} matrix row(s) have no zaaktype/vertrouwelijkheid tests: {string.Join(", ", uncovered)}."
        );
    }

    private static Dictionary<string, (Type Controller, MethodInfo Action)> ScopedActions()
    {
        return typeof(AnchorController)
            .Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes<BaseScopeAttribute>(inherit: true).Any())
                    .Select(m => (Controller: t, Action: m))
            )
            .ToDictionary(a => ScopeMatrixRow.NameOf(a.Controller, a.Action.Name));
    }

    // The scopes are a constructor argument the attribute keeps private, so they are read from its metadata.
    private static string[] ScopesOf(MethodInfo action)
    {
        var declaring = action.GetCustomAttributesData().Any(IsScope) ? action : action.GetBaseDefinition();
        var attribute = Assert.Single(declaring.GetCustomAttributesData(), IsScope);
        var scopes = (ReadOnlyCollection<CustomAttributeTypedArgument>)Assert.Single(attribute.ConstructorArguments).Value!;

        return scopes.Select(s => (string)s.Value).ToArray();
    }

    private static bool IsScope(CustomAttributeData attribute) => typeof(BaseScopeAttribute).IsAssignableFrom(attribute.AttributeType);
}
