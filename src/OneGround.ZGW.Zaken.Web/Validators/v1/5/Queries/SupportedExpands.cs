using System;
using System.Collections.Generic;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

public static class SupportedExpands
{
    public static IEnumerable<string> GetAll(string rootName)
    {
        if (rootName != null)
        {
            // Note: VNG specifies: The expand MUST not go deeper than a maximum of 3 levels deep.
            if (rootName.Equals("zaak", StringComparison.OrdinalIgnoreCase))
            {
                yield return "zaaktype";
                yield return "zaaktype.catalogus";

                yield return "status";
                yield return "status.statustype";

                yield return "resultaat";
                yield return "resultaat.resultaattype";

                yield return "hoofdzaak";
                yield return "hoofdzaak.zaaktype";
                yield return "hoofdzaak.zaaktype.catalogus";
                yield return "hoofdzaak.status";
                yield return "hoofdzaak.status.statustype";
                yield return "hoofdzaak.resultaat";
                yield return "hoofdzaak.resultaat.resultaattype";
                yield return "hoofdzaak.deelzaken";
                yield return "hoofdzaak.deelzaken.zaaktype";
                yield return "hoofdzaak.deelzaken.status";
                yield return "hoofdzaak.deelzaken.resultaat";
                yield return "hoofdzaak.rollen";
                yield return "hoofdzaak.rollen.roltype";
                yield return "hoofdzaak.zaakinformatieobjecten";
                yield return "hoofdzaak.zaakinformatieobjecten.informatieobject";
                yield return "hoofdzaak.zaakobjecten";

                yield return "deelzaken";
                yield return "deelzaken.zaaktype";
                yield return "deelzaken.zaaktype.catalogus";
                yield return "deelzaken.status";
                yield return "deelzaken.status.statustype";
                yield return "deelzaken.resultaat";
                yield return "deelzaken.resultaat.resultaattype";
                yield return "deelzaken.rollen";
                yield return "deelzaken.rollen.roltype";
                yield return "deelzaken.zaakinformatieobjecten";
                yield return "deelzaken.zaakinformatieobjecten.informatieobject";
                yield return "deelzaken.zaakobjecten";

                yield return "relevanteanderezaken";
                yield return "relevanteanderezaken.zaaktype";
                yield return "relevanteanderezaken.status";
                yield return "relevanteanderezaken.status.statustype";
                yield return "relevanteanderezaken.resultaat";
                yield return "relevanteanderezaken.resultaat.resultaattype";

                yield return "zaakinformatieobjecten";
                yield return "zaakinformatieobjecten.informatieobject";
                yield return "zaakinformatieobjecten.informatieobject.informatieobjecttype";

                yield return "eigenschappen";
                yield return "eigenschappen.eigenschap";
                yield return "rollen";
                yield return "rollen.roltype";
                yield return "zaakobjecten";
                yield return "zaakobjecten.zaakobjecttype";
                yield return "zaakverzoeken";
                yield return "zaakcontactmomenten";
            }
        }
    }
}
