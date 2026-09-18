using System;

namespace OneGround.ZGW.Common.Web.Authorization;

/// <summary>
/// Marks a controller or a controller action as deliberately not requiring a <see cref="BaseScopeAttribute"/>.
/// Without either of the two, <see cref="Filters.RequireScopeAuthorizationFilter"/> denies the request with a 403.
/// The reason is mandatory: it records in code why the endpoint is unscoped, so the exemption stays reviewable.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ScopeNotRequiredAttribute : Attribute
{
    public ScopeNotRequiredAttribute(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required to exempt an endpoint from scope authorization.", nameof(reason));

        Reason = reason;
    }

    /// <summary>
    /// Why this endpoint is deliberately unscoped.
    /// </summary>
    public string Reason { get; }
}
