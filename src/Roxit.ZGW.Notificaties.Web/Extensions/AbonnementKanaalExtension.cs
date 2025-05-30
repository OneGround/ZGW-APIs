using System.Collections.Generic;
using System.Linq;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Web.Extensions;

public static class AbonnementKanaalExtension
{
    public static IDictionary<string, string> FiltersToDictionary(this AbonnementKanaal abonnementkanaal)
    {
        return abonnementkanaal.Filters.ToDictionary(k => k.Key, v => v.Value);
    }
}
