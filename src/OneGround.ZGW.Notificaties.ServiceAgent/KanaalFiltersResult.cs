using System.Collections.Generic;

namespace OneGround.ZGW.Notificaties.ServiceAgent;

public class KanaalFiltersResult
{
    public KanaalFiltersResult(string naam, bool kanaalNotAvailable)
    {
        KanaalNotAvailable = kanaalNotAvailable;

        Naam = naam;
        Filters = [];
    }

    public KanaalFiltersResult(string naam, HashSet<string> filters)
    {
        KanaalNotAvailable = false;

        Naam = naam;
        Filters = filters;
    }

    public bool KanaalNotAvailable { get; }
    public string Naam { get; }
    public HashSet<string> Filters { get; }
}
