using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.Documenten.Messaging.Helper;

internal static class InformatieObjectParser
{
    public static KeyValuePair<string, string> ParseInformatieObject(IDictionary<string, string> kenmerken)
    {
        var zaakobject = kenmerken.SingleOrDefault(k => k.Key == "zaakinformatieobject.informatieobject");
        if (zaakobject.Key != null)
        {
            return zaakobject;
        }
        var besluitobject = kenmerken.SingleOrDefault(k => k.Key == "besluitinformatieobject.informatieobject");
        if (besluitobject.Key != null)
        {
            return besluitobject;
        }
        return default;
    }
}
