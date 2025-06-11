using System.Text.RegularExpressions;

namespace OneGround.ZGW.Common.Caching;

public class CacheEntity
{
    public static readonly CacheEntity ZaakType = new("ZTC", "zaaktypen");
    public static readonly CacheEntity StatusType = new("ZTC", "statustypen");
    public static readonly CacheEntity Eigenschap = new("ZTC", "eigenschappen");
    public static readonly CacheEntity RolType = new("ZTC", "roltypen");
    public static readonly CacheEntity ResultaatType = new("ZTC", "resultaattypen");
    public static readonly CacheEntity InformatieObjectType = new("ZTC", "informatieobjecttypen");
    public static readonly CacheEntity BesluitType = new("ZTC", "besluittypen");
    public static readonly CacheEntity ZaakObjectType = new("ZTC", "zaakobjecttypen");
    public static readonly CacheEntity Applicatie = new("AC", "applicaties", new Regex(@"/(?<entity>applicaties)/consumer", RegexOptions.IgnoreCase));

    /// <summary>
    /// Create an instance of cachable uri configuration.
    /// </summary>
    /// <param name="service">Service name used to match service endpoint.</param>
    /// <param name="entity">Entity name to cache: i.e. 'zaaktypen', 'roltypen'.</param>
    private CacheEntity(string service, string entity)
    {
        Service = service;
        Pattern = new Regex($@"/(?<entity>{entity})/(?<uuid>[a-z0-9\-]{{36}})$", RegexOptions.IgnoreCase);
        Entity = entity;
    }

    /// <summary>
    /// Create an instance of cachable uri configuration.
    /// </summary>
    /// <param name="service">Service name used to match service endpoint.</param>
    /// <param name="entity">Entity name to cache: i.e. 'zaaktypen', 'roltypen'.</param>
    /// <param name="pattern">URI pattern</param>
    private CacheEntity(string service, string entity, Regex pattern)
    {
        Service = service;
        Pattern = pattern;
        Entity = entity;
    }

    /// <summary>
    /// Service name used to match service endpoint.
    /// </summary>
    public string Service { get; }

    /// <summary>
    /// Pattern to match uri on.
    /// </summary>
    public Regex Pattern { get; }

    /// <summary>
    /// Entity name.
    /// </summary>
    public string Entity { get; }

    public override string ToString() => $"ZGW:{Service}:{Entity}";
}
