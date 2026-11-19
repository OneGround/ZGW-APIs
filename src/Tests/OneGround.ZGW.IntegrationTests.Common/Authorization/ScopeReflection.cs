using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Web.Authorization;
using Xunit;

namespace OneGround.ZGW.IntegrationTests.Common.Authorization;

/// <summary>
/// Reads the <c>[Scope]</c> attributes of a controller assembly from their metadata, so a scope table can be held to them.
/// </summary>
public static class ScopeReflection
{
    /// <summary>
    /// Every public controller action in <paramref name="controllerAssembly"/> that carries a scope attribute, keyed by what <paramref name="nameOf"/> makes of its controller and action name.
    /// </summary>
    public static Dictionary<string, (Type Controller, MethodInfo Action)> ScopedActions(
        Assembly controllerAssembly,
        Func<Type, string, string> nameOf
    )
    {
        return controllerAssembly
            .GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes<BaseScopeAttribute>(inherit: true).Any())
                    .Select(m => (Controller: t, Action: m))
            )
            .ToDictionary(a => nameOf(a.Controller, a.Action.Name));
    }

    /// <summary>
    /// The scopes of <paramref name="action"/>, read from the attribute on the action or on the base definition it overrides.
    /// </summary>
    public static string[] ScopesOf(MethodInfo action)
    {
        // The scopes are a constructor argument the attribute keeps private, so they are read from its metadata.
        var declaring = action.GetCustomAttributesData().Any(IsScope) ? action : action.GetBaseDefinition();
        var attribute = Assert.Single(declaring.GetCustomAttributesData(), IsScope);
        var scopes = (ReadOnlyCollection<CustomAttributeTypedArgument>)Assert.Single(attribute.ConstructorArguments).Value!;

        return scopes.Select(s => (string)s.Value).ToArray();
    }

    private static bool IsScope(CustomAttributeData attribute) => typeof(BaseScopeAttribute).IsAssignableFrom(attribute.AttributeType);
}
