using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Web.Authorization;
using Xunit;
using AnchorController = OneGround.ZGW.Documenten.Web.Controllers.v1.ObjectInformatieObjectenController;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.Authorization;

/// <summary>
/// Every controller action in the Drc API must carry a scope attribute, or an explicit
/// <see cref="ScopeNotRequiredAttribute"/> recording why it does not. RequireScopeAuthorizationFilter asks
/// <see cref="ScopeRequirement"/> the same question at runtime and answers a miss with a 403, so an action
/// listed by this test is an action that would be denied in production.
/// </summary>
public class DrcScopeCoverageTests
{
    [Fact]
    public void Every_controller_action_requires_a_scope_or_is_explicitly_exempt()
    {
        var actions = ControllerActions();

        // Guards a vacuous pass: reflection finding no controllers at all would leave the offender list empty too.
        Assert.NotEmpty(actions);

        var offenders = actions
            .Where(a => !ScopeRequirement.IsSatisfiedBy(a.Controller, a.Action))
            .Select(a => $"{a.Controller.FullName}.{a.Action.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} Drc controller action(s) carry neither a scope attribute nor "
                + $"[ScopeNotRequired(\"...\")], so they are denied with a 403 at runtime: {string.Join(", ", offenders)}."
        );

        // ScopeRequirement reads attribute metadata, which never runs ScopeNotRequiredAttribute's constructor,
        // so [ScopeNotRequired("")] satisfies the check above. Materializing each exemption runs that constructor.
        var malformed = new List<string>();

        foreach (
            var carrier in actions
                .SelectMany(a => new MemberInfo[] { a.Action, a.Controller })
                .Distinct()
                .Where(m => m.IsDefined(typeof(ScopeNotRequiredAttribute), inherit: true))
        )
        {
            try
            {
                _ = carrier.GetCustomAttribute<ScopeNotRequiredAttribute>(inherit: true);
            }
            catch (ArgumentException)
            {
                malformed.Add((carrier as Type)?.FullName ?? $"{carrier.DeclaringType?.FullName}.{carrier.Name}");
            }
        }

        Assert.True(
            malformed.Count == 0,
            $"{malformed.Count} Drc exemption(s) carry an empty or whitespace reason, which the attribute's constructor "
                + $"rejects and a running host rejects while building its action descriptors: {string.Join(", ", malformed)}."
        );
    }

    private static List<(Type Controller, MethodInfo Action)> ControllerActions() =>
        typeof(AnchorController)
            .Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => ActionsOf(t).Select(action => (Controller: t, Action: action)))
            .ToList();

    private static IEnumerable<MethodInfo> ActionsOf(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(IsAction);

    private static bool IsAction(MethodInfo method)
    {
        if (method.IsSpecialName || method.IsStatic || method.IsAbstract || method.IsGenericMethodDefinition)
            return false;

        if (method.IsDefined(typeof(NonActionAttribute), inherit: true))
            return false;

        // Methods the framework declares - ControllerBase.Ok(), object.Equals() - are never actions.
        var declaringAssembly = method.GetBaseDefinition().DeclaringType?.Assembly;

        return declaringAssembly != null
            && declaringAssembly != typeof(object).Assembly
            && !declaringAssembly.GetName().Name.StartsWith("Microsoft.", StringComparison.Ordinal);
    }
}
