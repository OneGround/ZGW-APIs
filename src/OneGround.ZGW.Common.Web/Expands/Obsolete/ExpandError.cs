using System;

namespace OneGround.ZGW.Common.Web.Expands;

[Obsolete("This class is obsolete. Use the new expand/field-selection mechanism instead.")]
public static class ExpandError
{
    public static object Create(object context) => new { _error = context };
}
