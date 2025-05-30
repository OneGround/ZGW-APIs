using System;
using System.Collections.Generic;

namespace Roxit.ZGW.Notificaties.DataModel;

public class Notificatie
{
    public string Kanaal { get; set; }
    public string HoofdObject { get; set; }
    public string Resource { get; set; }
    public string ResourceUrl { get; set; }
    public string Actie { get; set; }
    public DateTime AanmaakDatum { get; set; }
    public IDictionary<string, string> Kenmerken { get; set; }
}
