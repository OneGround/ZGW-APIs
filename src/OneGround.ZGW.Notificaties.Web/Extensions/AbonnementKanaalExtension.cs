using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Web.Extensions;

public static class AbonnementKanaalExtension
{
    public static IDictionary<string, string> FiltersToDictionary(this AbonnementKanaal abonnementkanaal)
    {
        return abonnementkanaal.Filters.ToDictionary(k => k.Key, v => v.Value);
    }
}
