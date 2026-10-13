using System.Collections.Generic;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

public class FieldSelection
{
    public HashSet<string> ScalarFields { get; set; } = new();

    /// <summary>
    /// Expand sub-entities (object syntax <c>{"name": [...]}</c>): fetched by a resolver
    /// and placed in <c>_expand</c>.
    /// </summary>
    public Dictionary<string, FieldSelection> Entities { get; set; } = new();

    /// <summary>
    /// Inline nested objects (dotted syntax <c>name.field</c>): already part of the
    /// response object and projected in place, not via expand.
    /// </summary>
    public Dictionary<string, FieldSelection> NestedObjects { get; set; } = new();

    public bool IncludeAllScalars { get; set; } = false;
}
