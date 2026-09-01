using System.Collections.Generic;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

public class FieldSelection
{
    public HashSet<string> ScalarFields { get; set; } = new();

    /// <summary>
    /// Expand-sub-entiteiten (object-syntax <c>{"naam": [...]}</c>): worden via een resolver
    /// opgehaald en in <c>_expand</c> geplaatst.
    /// </summary>
    public Dictionary<string, FieldSelection> Entities { get; set; } = new();

    /// <summary>
    /// Inline geneste objecten (gepunte syntax <c>naam.veld</c>): al onderdeel van het
    /// response-object en worden ter plekke (in-place) geprojecteerd, niet via expand.
    /// </summary>
    public Dictionary<string, FieldSelection> NestedObjects { get; set; } = new();

    public bool IncludeAllScalars { get; set; } = false;
}
