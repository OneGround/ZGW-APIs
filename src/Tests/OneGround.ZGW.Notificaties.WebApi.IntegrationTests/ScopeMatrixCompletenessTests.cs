using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OneGround.ZGW.IntegrationTests.Common.Authorization;
using Xunit;
using AnchorController = OneGround.ZGW.Notificaties.Web.Controllers.v1.NotificatiesController;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Holds <see cref="ScopeMatrix"/> to the <c>[Scope]</c> attributes it was copied from, reading attribute metadata only.
/// </summary>
public class ScopeMatrixCompletenessTests
{
    [Fact]
    public void Every_scoped_action_has_exactly_one_matrix_row()
    {
        var actions = ScopeReflection.ScopedActions(typeof(AnchorController).Assembly, ScopeMatrixRow.NameOf);

        // Guards a vacuous pass: reflection finding no scoped actions would leave both lists empty too.
        Assert.Equal(10, actions.Count);

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
    public void Every_matrix_row_names_the_scopes_and_route_of_its_action()
    {
        var actions = ScopeReflection.ScopedActions(typeof(AnchorController).Assembly, ScopeMatrixRow.NameOf);
        var mismatches = new List<string>();

        foreach (var row in ScopeMatrix.Rows.Where(r => actions.ContainsKey(r.Name)))
        {
            var (_, action) = actions[row.Name];

            var scopes = ScopeReflection.ScopesOf(action);
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
        }

        Assert.True(
            mismatches.Count == 0,
            $"{mismatches.Count} matrix row(s) differ from their action:{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}"
        );
    }
}
