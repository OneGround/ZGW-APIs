using System;

namespace OneGround.ZGW.Common.Web.Expands;

// Prevents the attribute to be invoked twice
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class Expand : Attribute { }
