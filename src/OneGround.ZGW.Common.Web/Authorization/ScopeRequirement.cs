using System;
using System.Reflection;

namespace OneGround.ZGW.Common.Web.Authorization;

/// <summary>
/// The single decision "is this controller action allowed to be reached?", shared by
/// <see cref="Filters.RequireScopeAuthorizationFilter"/> at runtime and by the per-API scope-coverage unit tests.
/// Both call this, so a failing coverage test means exactly what a 403 from the filter would mean.
/// </summary>
public static class ScopeRequirement
{
    /// <summary>
    /// True when the action, or the controller declaring it, carries either a <see cref="BaseScopeAttribute"/>
    /// (the scope check itself) or a <see cref="ScopeNotRequiredAttribute"/> (an explicit, reasoned exemption).
    /// </summary>
    public static bool IsSatisfiedBy(Type controllerType, MethodInfo action)
    {
        ArgumentNullException.ThrowIfNull(controllerType);
        ArgumentNullException.ThrowIfNull(action);

        return HasScope(action) || HasExemption(action) || HasScope(controllerType) || HasExemption(controllerType);
    }

    private static bool HasScope(MemberInfo member) => member.IsDefined(typeof(BaseScopeAttribute), inherit: true);

    private static bool HasExemption(MemberInfo member) => member.IsDefined(typeof(ScopeNotRequiredAttribute), inherit: true);
}
