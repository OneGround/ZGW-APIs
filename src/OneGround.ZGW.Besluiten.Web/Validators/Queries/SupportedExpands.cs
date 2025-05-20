using System;
using System.Collections.Generic;
using OneGround.ZGW.Besluiten.Web.Expands;

namespace OneGround.ZGW.Besluiten.Web.Validators.Queries;

public static class SupportedExpands
{
    public static IEnumerable<string> GetAll(string rootName)
    {
        if (rootName == null)
            yield break;

        if (!rootName.Equals(ExpanderNames.BesluitExpander, StringComparison.OrdinalIgnoreCase))
            yield break;

        yield return ExpandKeys.BesluitType;
        yield return ExpandQueries.BesluitType_Catalogus;

        yield return ExpandKeys.BesluitInformatieObjecten;
        yield return ExpandQueries.BesluitInformatieObjecten_InformatieObject;
        yield return ExpandQueries.BesluitInformatieObjecten_InformatieObject_InformatieObjectType;
    }
}
