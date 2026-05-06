using System;

namespace OneGround.ZGW.Common.Contracts.v1.AuditTrail;

/// <summary>
/// Marks a property as containing sensitive data that should be masked in legacy and delta based audit trail logs.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class AuditMaskFieldAttribute : Attribute { }
