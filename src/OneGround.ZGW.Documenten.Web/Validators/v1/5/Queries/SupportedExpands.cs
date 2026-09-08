using System;
using System.Collections.Generic;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._5.Queries;

[Obsolete("This class is obsolete. Use the new expand/field-selection mechanism instead.")]
public static class SupportedExpands
{
    public static IEnumerable<string> GetAll(string rootName)
    {
        // Note: VNG specifies: The expand MUST not go deeper than a maximum of 3 levels deep.
        if (rootName != null)
        {
            if (rootName.Equals("enkelvoudiginformatieobject", StringComparison.OrdinalIgnoreCase))
            {
                yield return "informatieobjecttype";
                yield return "informatieobjecttype.catalogus";
            }
            else if (
                rootName.Equals("gebruiksrecht", StringComparison.OrdinalIgnoreCase)
                || rootName.Equals("verzending", StringComparison.OrdinalIgnoreCase)
                || rootName.Equals("objectinformatieobject", StringComparison.OrdinalIgnoreCase)
            )
            {
                yield return "informatieobject";
                yield return "informatieobject.informatieobjecttype";
                yield return "informatieobject.informatieobjecttype.catalogus"; // Note: Probably we want not support this but VNG reference does
            }
        }
    }
}
