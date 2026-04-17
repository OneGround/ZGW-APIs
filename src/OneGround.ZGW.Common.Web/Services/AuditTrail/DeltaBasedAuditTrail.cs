using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public class DeltaBasedAuditTrail : IAuditTrailService
{
    public const string PropertiesUsingCurrentValue = "PropertiesUsingCurrentValue";
    public const string ForceUseSnapshotWhenResourceChanged = "ForceUseSnapshotWhenResourceChanged"; // TODO: Not implement yet
    public const string SnapshotInterval = "SnapshotInterval"; // TODO: Not implement yet

    protected const int _snapshotInterval = 25;

    protected readonly IDbContextWithAuditTrail _context;
    protected readonly IMapper _mapper;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly IEntityUriService _uriService;

    private string _oldJson;
    private string _newJson;

    protected AuditTrailOptions _options = new AuditTrailOptions();

    public DeltaBasedAuditTrail(
        IDbContextWithAuditTrail context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IEntityUriService uriService
    )
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _uriService = uriService;
    }

    public virtual string Name => "Deltas";

    public void SetOptions(AuditTrailOptions options)
    {
        _options = options;
    }

    public void SetOld<TDto>(IBaseEntity entity)
    {
        var oldDto = _mapper.Map<TDto>(entity);

        _oldJson = ToJson(oldDto);
    }

    public void SetNew<TDto>(IBaseEntity entity)
    {
        var newDto = _mapper.Map<TDto>(entity);

        _newJson = ToJson(newDto);
    }

    public Task CreatedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            AuditActie.create,
            "Object aangemaakt",
            entity,
            _uriService.GetUri(subEntity),
            HttpStatusCode.Created,
            cancellationToken: cancellationToken
        );
    }

    public Task GetAsync(IBaseEntity entity, IUrlEntity subEntity, string overruleActieWeergave = null, CancellationToken cancellationToken = default)
    {
        var actieWeergave = string.IsNullOrEmpty(overruleActieWeergave) ? "Object gelezen" : overruleActieWeergave;

        return WriteAsync(
            AuditActie.retrieve,
            actieWeergave,
            entity,
            _uriService.GetUri(subEntity),
            HttpStatusCode.OK,
            dontWriteEntity: false,
            cancellationToken: cancellationToken
        );
    }

    public Task GetListAsync(int count, int totalCount, int page, CancellationToken cancellationToken = default)
    {
        return totalCount == 0
            ? WriteAsync(
                AuditActie.retrieve,
                "Lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: "Lijst bevat geen objecten",
                cancellationToken: cancellationToken
            )
            : WriteAsync(
                AuditActie.retrieve,
                "Lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: $"Lijst van {count} van {totalCount} objecten gelezen (pagina {page})",
                cancellationToken: cancellationToken
            );
    }

    public Task GetListAsync(int totalCount, CancellationToken cancellationToken = default)
    {
        return totalCount == 0
            ? WriteAsync(
                AuditActie.retrieve,
                "Lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: "Lijst bevat geen objecten",
                cancellationToken: cancellationToken
            )
            : WriteAsync(
                AuditActie.retrieve,
                "Lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: $"Lijst van {totalCount} objecten gelezen",
                cancellationToken: cancellationToken
            );
    }

    public Task DestroyedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            AuditActie.destroy,
            "Object verwijderd",
            entity,
            _uriService.GetUri(subEntity),
            HttpStatusCode.NoContent,
            cancellationToken: cancellationToken
        );
    }

    public Task DestroyedAsync(IUrlEntity entity, string toelichting, CancellationToken cancellationToken = default)
    {
        var hoofdobject = _uriService.GetUri(entity);

        return WriteAsync(
            AuditActie.destroy,
            "Object verwijderd",
            hoofdobject,
            hoofdobject,
            HttpStatusCode.NoContent,
            cancellationToken: cancellationToken,
            toelichting: toelichting
        );
    }

    public Task UpdatedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            AuditActie.update,
            "Object bijgewerkt",
            entity,
            _uriService.GetUri(subEntity),
            HttpStatusCode.OK,
            cancellationToken: cancellationToken
        );
    }

    public Task PatchedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            AuditActie.partial_update,
            "Object bijgewerkt",
            entity,
            _uriService.GetUri(subEntity),
            HttpStatusCode.OK,
            cancellationToken: cancellationToken
        );
    }

    public Task PatchedAsync(IBaseEntity entity, IUrlEntity subEntity, string toelichting, CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            AuditActie.partial_update,
            "Object bijgewerkt",
            entity,
            _uriService.GetUri(subEntity),
            HttpStatusCode.OK,
            toelichting: toelichting ?? "",
            cancellationToken: cancellationToken
        );
    }

    public async Task<IEnumerable<AuditTrailRegel>> GetAuditTrailEntriesAsync(Guid hoofdobjectId, CancellationToken cancellationToken = default)
    {
        var auditsWithAllResources = await _context
            .AuditTrailDeltas.AsNoTracking()
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == hoofdobjectId)
            .OrderBy(a => a.ResourceId)
            .ThenBy(a => a.Versie)
            .ToListAsync(cancellationToken);

        var auditsGroupedByResource = auditsWithAllResources.ToLookup(a => a.ResourceId);

        var result = new List<AuditTrailRegel>();

        //
        // Rebuild the complete audittrail starting with snapshot and applying deltas....

        foreach (var auditsForResource in auditsGroupedByResource)
        {
            JsonNode current = null;
            foreach (var audit in auditsForResource)
            {
                JsonDocument oldVersion = current != null ? current.Deserialize<JsonDocument>() : default;
                var oldVersionJson = ToJson(oldVersion);

                if (audit.Actie == $"{AuditActie.create}")
                {
                    current = JsonNode.Parse(audit.SnapshotJson!);
                }
                else if (audit.Actie == $"{AuditActie.update}" || audit.Actie == $"{AuditActie.partial_update}")
                {
                    // Handle periodic snapshots
                    if (!string.IsNullOrEmpty(audit.SnapshotJson))
                    {
                        current = JsonNode.Parse(audit.SnapshotJson!);
                    }
                    else
                    {
                        var delta = JsonNode.Parse(audit.DeltaJson!)!.AsObject();
                        ApplyDelta(current!.AsObject(), delta);
                    }
                }
                else if (audit.Actie == $"{AuditActie.destroy}")
                {
                    var deletedVersion = current!.Deserialize<JsonDocument>();
                    var deletedVersionJson = ToJson(deletedVersion);

                    result.Add(
                        new AuditTrailRegel
                        {
                            Id = audit.Id,
                            AanmaakDatum = audit.AanmaakDatum,
                            Actie = audit.Actie,
                            ActieWeergave = audit.ActieWeergave,
                            ApplicatieId = audit.ApplicatieId,
                            ApplicatieWeergave = audit.ApplicatieWeergave,
                            Bron = audit.Bron,
                            GebruikersId = audit.GebruikersId,
                            GebruikersWeergave = audit.GebruikersWeergave,
                            HoofdObject = audit.HoofdObject,
                            HoofdObjectId = audit.HoofdObjectId,
                            Resource = audit.Resource,
                            ResourceUrl = audit.ResourceUrl,
                            ResourceWeergave = audit.ResourceWeergave,
                            RequestId = audit.RequestId,
                            Resultaat = audit.Resultaat,
                            Toelichting = audit.Toelichting,
                            // Reconstructed object
                            Nieuw = default,
                            Oud = deletedVersion != null ? JsonSerializer.SerializeToNode(deletedVersion)?.ToString() : null,
                        }
                    );
                    current = null;
                    continue;
                }
                else if (audit.Actie == $"{AuditActie.retrieve}")
                {
                    result.Add(
                        new AuditTrailRegel
                        {
                            Id = audit.Id,
                            AanmaakDatum = audit.AanmaakDatum,
                            Actie = audit.Actie,
                            ActieWeergave = audit.ActieWeergave,
                            ApplicatieId = audit.ApplicatieId,
                            ApplicatieWeergave = audit.ApplicatieWeergave,
                            Bron = audit.Bron,
                            GebruikersId = audit.GebruikersId,
                            GebruikersWeergave = audit.GebruikersWeergave,
                            HoofdObject = audit.HoofdObject,
                            HoofdObjectId = audit.HoofdObjectId,
                            Resource = audit.Resource,
                            ResourceUrl = audit.ResourceUrl,
                            ResourceWeergave = audit.ResourceWeergave,
                            RequestId = audit.RequestId,
                            Resultaat = audit.Resultaat,
                            Toelichting = audit.Toelichting,
                            // Log retrieve operation only
                            Nieuw = default,
                            Oud = default,
                        }
                    );
                    continue;
                }

                JsonDocument newVersion = current != null ? current.Deserialize<JsonDocument>() : default;
                var newVersionJson = ToJson(newVersion);

                if (oldVersion != null || newVersion != null)
                {
                    result.Add(
                        new AuditTrailRegel
                        {
                            Id = audit.Id,
                            AanmaakDatum = audit.AanmaakDatum,
                            Actie = audit.Actie,
                            ActieWeergave = audit.ActieWeergave,
                            ApplicatieId = audit.ApplicatieId,
                            ApplicatieWeergave = audit.ApplicatieWeergave,
                            Bron = audit.Bron,
                            GebruikersId = audit.GebruikersId,
                            GebruikersWeergave = audit.GebruikersWeergave,
                            HoofdObject = audit.HoofdObject,
                            HoofdObjectId = audit.HoofdObjectId,
                            Resource = audit.Resource,
                            ResourceUrl = audit.ResourceUrl,
                            ResourceWeergave = audit.ResourceWeergave,
                            RequestId = audit.RequestId,
                            Resultaat = audit.Resultaat,
                            Toelichting = audit.Toelichting,
                            // Reconstructed object
                            Nieuw = newVersion != null ? JsonSerializer.SerializeToNode(newVersion)?.ToString() : null,
                            Oud = oldVersion != null ? JsonSerializer.SerializeToNode(oldVersion)?.ToString() : null,
                        }
                    );
                }
            }
        }

        return result.OrderBy(a => a.AanmaakDatum);
    }

    public async Task<AuditTrailRegel> GetAuditTrailEntryByIdAsync(
        Guid hoofdobjectId,
        Guid audittrailId,
        CancellationToken cancellationToken = default
    )
    {
        // Use a subquery to resolve ResourceId and Versie, combining both queries into a single database round-trip
        var requestedAuditSubquery = _context
            .AuditTrailDeltas.AsNoTracking()
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == hoofdobjectId && a.Id == audittrailId);

        var audits = await _context
            .AuditTrailDeltas.AsNoTracking()
            .Where(a =>
                a.HoofdObjectId.HasValue
                && a.HoofdObjectId == hoofdobjectId
                && a.ResourceId.HasValue
                && requestedAuditSubquery.Any(r => r.ResourceId == a.ResourceId && a.Versie <= r.Versie)
            )
            .OrderBy(a => a.Versie)
            .ToListAsync(cancellationToken);

        AuditTrailRegel auditTrailRegel = null;

        // First try to find the requested audittrail regel as 'retrieve' operation, if found return immediately (without rebuilding the object state)
        var retrieveAudit = audits.SingleOrDefault(a => a.Id == audittrailId && a.Actie == $"{AuditActie.retrieve}");
        if (retrieveAudit != null)
        {
            auditTrailRegel = new AuditTrailRegel
            {
                Id = retrieveAudit.Id,
                AanmaakDatum = retrieveAudit.AanmaakDatum,
                Actie = retrieveAudit.Actie,
                ActieWeergave = retrieveAudit.ActieWeergave,
                ApplicatieId = retrieveAudit.ApplicatieId,
                ApplicatieWeergave = retrieveAudit.ApplicatieWeergave,
                Bron = retrieveAudit.Bron,
                GebruikersId = retrieveAudit.GebruikersId,
                GebruikersWeergave = retrieveAudit.GebruikersWeergave,
                HoofdObject = retrieveAudit.HoofdObject,
                HoofdObjectId = retrieveAudit.HoofdObjectId,
                Resource = retrieveAudit.Resource,
                ResourceUrl = retrieveAudit.ResourceUrl,
                ResourceWeergave = retrieveAudit.ResourceWeergave,
                RequestId = retrieveAudit.RequestId,
                Resultaat = retrieveAudit.Resultaat,
                Toelichting = retrieveAudit.Toelichting,
                // Log retrieve operation only
                Nieuw = default,
                Oud = default,
            };
            return auditTrailRegel;
        }

        //
        // Rebuild the requested audittrail regel starting with snapshot and applying deltas....

        JsonNode current = null;
        foreach (var audit in audits.Where(a => a.Actie != $"{AuditActie.retrieve}"))
        {
            JsonDocument oldVersion = current != null ? current.Deserialize<JsonDocument>() : default;
            var oldVersionJson = ToJson(oldVersion);

            if (audit.Actie == $"{AuditActie.create}")
            {
                current = JsonNode.Parse(audit.SnapshotJson!);
            }
            else if (audit.Actie == $"{AuditActie.update}" || audit.Actie == $"{AuditActie.partial_update}")
            {
                // Handle periodic snapshots
                if (!string.IsNullOrEmpty(audit.SnapshotJson))
                {
                    current = JsonNode.Parse(audit.SnapshotJson);
                }
                else
                {
                    var delta = JsonNode.Parse(audit.DeltaJson).AsObject();
                    ApplyDelta(current.AsObject(), delta);
                }
            }
            else if (audit.Actie == $"{AuditActie.destroy}")
            {
                var deletedVersion = current!.Deserialize<JsonDocument>();
                var deletedVersionJson = ToJson(deletedVersion);

                auditTrailRegel = new AuditTrailRegel
                {
                    Id = audit.Id,
                    AanmaakDatum = audit.AanmaakDatum,
                    Actie = audit.Actie,
                    ActieWeergave = audit.ActieWeergave,
                    ApplicatieId = audit.ApplicatieId,
                    ApplicatieWeergave = audit.ApplicatieWeergave,
                    Bron = audit.Bron,
                    GebruikersId = audit.GebruikersId,
                    GebruikersWeergave = audit.GebruikersWeergave,
                    HoofdObject = audit.HoofdObject,
                    HoofdObjectId = audit.HoofdObjectId,
                    Resource = audit.Resource,
                    ResourceUrl = audit.ResourceUrl,
                    ResourceWeergave = audit.ResourceWeergave,
                    RequestId = audit.RequestId,
                    Resultaat = audit.Resultaat,
                    Toelichting = audit.Toelichting,
                    // Reconstructed object
                    Nieuw = default,
                    Oud = deletedVersion != null ? JsonSerializer.SerializeToNode(deletedVersion)?.ToString() : null,
                };

                current = null;
                continue;
            }
            else
            {
                // Note: In case of 'retrieve' operations we do not want to reconstruct the object state. Is is handle separately at the beginning of this method.
                continue;
            }

            JsonDocument newVersion = current != null ? current.Deserialize<JsonDocument>() : default;
            var newVersionJson = ToJson(newVersion);

            if (oldVersion != null || newVersion != null)
            {
                auditTrailRegel = new AuditTrailRegel
                {
                    Id = audit.Id,
                    AanmaakDatum = audit.AanmaakDatum,
                    Actie = audit.Actie,
                    ActieWeergave = audit.ActieWeergave,
                    ApplicatieId = audit.ApplicatieId,
                    ApplicatieWeergave = audit.ApplicatieWeergave,
                    Bron = audit.Bron,
                    GebruikersId = audit.GebruikersId,
                    GebruikersWeergave = audit.GebruikersWeergave,
                    HoofdObject = audit.HoofdObject,
                    HoofdObjectId = audit.HoofdObjectId,
                    Resource = audit.Resource,
                    ResourceUrl = audit.ResourceUrl,
                    ResourceWeergave = audit.ResourceWeergave,
                    RequestId = audit.RequestId,
                    Resultaat = audit.Resultaat,
                    Toelichting = audit.Toelichting,
                    // Reconstructed object
                    Nieuw = newVersion != null ? JsonSerializer.SerializeToNode(newVersion)?.ToString() : null,
                    Oud = oldVersion != null ? JsonSerializer.SerializeToNode(oldVersion)?.ToString() : null,
                };
            }
        }

        return auditTrailRegel;
    }

    private async Task WriteAsync(
        AuditActie auditActie,
        string actieWeergave,
        string hoofdobject,
        string resourceUrl,
        HttpStatusCode resultaat,
        string resourceWeergave = "",
        string toelichting = "",
        CancellationToken cancellationToken = default
    )
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var audittrail = new AuditTrailDelta
        {
            AanmaakDatum = DateTime.UtcNow,
            DeltaJson = null,
            SnapshotJson = null,
            HoofdObject = hoofdobject,
            ApplicatieId = httpContext.GetClientId(),
            ApplicatieWeergave = httpContext.GetApplicationLabel(),
            GebruikersId = httpContext.GetUserId(),
            GebruikersWeergave = httpContext.GetUserRepresentation(),
            Bron = _options.Bron,
            RequestId = httpContext.GetRequestId(),
            Actie = $"{auditActie}",
            ActieWeergave = actieWeergave,
            Resource = _options.Resource,
            ResourceUrl = resourceUrl,
            ResourceWeergave = resourceWeergave,
            Resultaat = (int)resultaat,
            Toelichting = toelichting,
        };

        await _context.AuditTrailDeltas.AddAsync(audittrail, cancellationToken);

        Reset();
    }

    private async Task WriteAsync(
        AuditActie actie,
        string actieWeergave,
        IBaseEntity hoofdobject,
        string resourceUrl,
        HttpStatusCode resultaat,
        string resourceWeergave = "",
        string toelichting = "",
        bool dontWriteEntity = false,
        CancellationToken cancellationToken = default
    )
    {
        if (hoofdobject is not IUrlEntity hoofdobjectUrl)
            throw new InvalidOperationException($"Entity {hoofdobject} expected to be an {nameof(IUrlEntity)}.");

        var entityUrl = _uriService.GetUri(hoofdobjectUrl);

        var resourceId = _uriService.GetId(resourceUrl);

        var httpContext = _httpContextAccessor.HttpContext;

        var audittrail = new AuditTrailDelta
        {
            AanmaakDatum = DateTime.UtcNow,
            DeltaJson = null,
            SnapshotJson = null,
            HoofdObject = entityUrl,
            ApplicatieId = httpContext.GetClientId(),
            ApplicatieWeergave = httpContext.GetApplicationLabel(),
            GebruikersId = httpContext.GetUserId(),
            GebruikersWeergave = httpContext.GetUserRepresentation(),
            Bron = _options.Bron,
            RequestId = httpContext.GetRequestId(),
            Actie = $"{actie}",
            ActieWeergave = actieWeergave,
            Resource = _options.Resource,
            ResourceUrl = resourceUrl,
            ResourceWeergave = resourceWeergave,
            Resultaat = (int)resultaat,
            Toelichting = toelichting,
            HoofdObjectId = hoofdobject.Id,
            ResourceId = resourceId,
        };

        if (!dontWriteEntity)
        {
            bool result = await ResolveSnapshotOrDeltaAsync(hoofdobject.Id, actie, _newJson, _oldJson, audittrail, cancellationToken);
            if (!result)
            {
                // No changes → Do not log
                return;
            }
        }

        await _context.AuditTrailDeltas.AddAsync(audittrail, cancellationToken);

        Reset();
    }

    protected async Task<bool> ResolveSnapshotOrDeltaAsync(
        Guid hoofdObjectId,
        AuditActie actie,
        string nieuw,
        string oud,
        AuditTrailDelta delta,
        CancellationToken cancellationToken
    )
    {
        switch (actie)
        {
            case AuditActie.create:
            {
                delta.Versie = 1;
                delta.SnapshotJson = nieuw;
                break;
            }

            case AuditActie.destroy:
            {
                delta.Versie = await GetNextVersionAsync(hoofdObjectId, delta.ResourceId.Value, cancellationToken);
                delta.DeltaJson = oud;
                break;
            }

            case AuditActie.update:
            case AuditActie.partial_update:
            {
                var original = JsonSerializer.Deserialize<JsonObject>(oud);
                var current = JsonSerializer.Deserialize<JsonObject>(nieuw);

                // Genereer delta
                var _delta = AuditDeltaGenerator.GenerateDelta(original, current, GetPropertiesUsingCurrentValues());

                // No changes → Do not log
                if (_delta == null || _delta.Count == 0)
                    return false;

                var versie = await GetNextVersionAsync(hoofdObjectId, delta.ResourceId.Value, cancellationToken);

                bool forcingSnapshot = false;
                if (GetForceUseSnapshotWhenResourceChanged())
                {
                    forcingSnapshot = await ShouldForceSnapshot(hoofdObjectId, delta.ResourceId.Value, cancellationToken);
                }

                /// Check if this is a snapshot version
                bool isSnapshotVersion = versie % _snapshotInterval == 0 || forcingSnapshot; // Or if forcingSnapshot is set

                delta.DeltaJson = isSnapshotVersion ? null : _delta.ToJsonString();
                delta.SnapshotJson = isSnapshotVersion ? nieuw : null;
                delta.Versie = versie;
                break;
            }

            case AuditActie.retrieve:
            {
                // No delta for reads, only snapshot
                delta.SnapshotJson = nieuw;
                delta.Versie = 0;
                break;
            }
        }
        return true;
    }

    private List<string> GetPropertiesUsingCurrentValues()
    {
        List<string> propertiesUsingCurrentValue = new();
        if (_options.Properties != null && _options.Properties.TryGetValue(PropertiesUsingCurrentValue, out var properties))
        {
            propertiesUsingCurrentValue = properties as List<string> ?? new List<string>();
        }

        return propertiesUsingCurrentValue;
    }

    private bool GetForceUseSnapshotWhenResourceChanged()
    {
        if (
            _options.Properties != null
            && _options.Properties.TryGetValue(ForceUseSnapshotWhenResourceChanged, out var value)
            && bool.TryParse($"{value}", out var forceUseSnapshotWhenResourceChanged)
            && forceUseSnapshotWhenResourceChanged
        )
        {
            return true;
        }

        return false;
    }

    protected async Task<int> GetNextVersionAsync(Guid hoofdObjectId, Guid resourceId, CancellationToken cancellationToken)
    {
        var last =
            await _context
                .AuditTrailDeltas.Where(a => a.HoofdObjectId == hoofdObjectId && a.ResourceId == resourceId)
                .MaxAsync(a => (int?)a.Versie, cancellationToken)
            ?? 0;

        return last + 1;
    }

    private async Task<bool> ShouldForceSnapshot(Guid hoofdObjectId, Guid resourceId, CancellationToken cancellationToken)
    {
        if (hoofdObjectId != resourceId)
            return false;

        var allResources = await GetAllResourcesByHoofdObjectIdAsync(hoofdObjectId, cancellationToken);
        // Note: top are the latest versions, bottom are the oldest versions

        var previous = allResources.FirstOrDefault();

        return previous?.ResourceId != resourceId;

        //AuditTrailDelta previous = null;
        //foreach (var resource in allResources)
        //{
        //    if (resource.HoofdObjectId == resourceId)
        //        break;

        //    previous = resource;
        //}
        //return previous == null;
    }

    // TODO:
    private async Task<IList<AuditTrailDelta>> GetAllResourcesByHoofdObjectIdAsync(Guid hoofdObjectId, CancellationToken cancellationToken)
    {
        var resources = await _context
            .AuditTrailDeltas.Where(a => a.HoofdObjectId == hoofdObjectId)
            .AsNoTracking()
            .OrderByDescending(a => a.AanmaakDatum)
            .ToListAsync(cancellationToken);

        return resources;
    }

    /// <summary>
    /// Applies a delta object to a target object, handling three special marker types:
    /// 1. "__removed": Property should be deleted from target (property was removed)
    /// 2. "__replace": Entire value should replace target value (prevents merging, used for PropertiesUsingCurrentValue)
    /// 3. Array delta markers (added/removed/updated): Array-specific delta operations
    /// </summary>
    private void ApplyDelta(JsonObject target, JsonObject delta)
    {
        foreach (var kv in delta)
        {
            var key = kv.Key;
            var value = kv.Value;

            // Handle special marker: __removed
            // Purpose: Property no longer exists in the object
            // Action: Remove the property from target
            if (value is JsonObject obj && obj.ContainsKey("__removed"))
            {
                target.Remove(key);
            }
            // Handle special marker: __replace
            // Purpose: Property value should completely replace existing value (no merging)
            // Action: Replace entire property value with the wrapped value
            // Used by: PropertiesUsingCurrentValue feature
            else if (value is JsonObject obj3 && obj3.ContainsKey("__replace"))
            {
                var replacementValue = obj3["__replace"];
                target[key] = replacementValue?.DeepClone();
            }
            else if (value is JsonObject obj2)
            {
                if (obj2.ContainsKey("added") || obj2.ContainsKey("removed") || obj2.ContainsKey("updated"))
                {
                    var array = target[key] as JsonArray ?? new JsonArray();
                    ApplyArrayDelta(array, obj2);
                    target[key] = array;
                }
                else
                {
                    var nested = target[key] as JsonObject ?? new JsonObject();
                    ApplyDelta(nested, obj2);
                    target[key] = nested;
                }
            }
            else
            {
                // This handles both setting to null and setting to other values
                target[key] = value?.DeepClone();
            }
        }
    }

    private void ApplyArrayDelta(JsonArray target, JsonObject delta)
    {
        if (delta.TryGetPropertyValue("removed", out var removedNode))
        {
            foreach (var r in removedNode.AsArray())
            {
                if (r is JsonObject rObj)
                {
                    var idx = FindMatchIndex(target, rObj);
                    if (idx >= 0)
                        target.RemoveAt(idx);
                }
                else
                {
                    var idx = FindPrimitiveIndex(target, r);
                    if (idx >= 0)
                        target.RemoveAt(idx);
                }
            }
        }

        if (delta.TryGetPropertyValue("added", out var addedNode))
        {
            foreach (var a in addedNode.AsArray())
                target.Add(a.DeepClone());
        }

        if (delta.TryGetPropertyValue("updated", out var updatedNode))
        {
            foreach (var u in updatedNode.AsArray())
            {
                if (u is JsonObject updateObj)
                {
                    var idx = FindMatchIndex(target, updateObj);

                    if (idx >= 0 && target[idx] is JsonObject targetObj)
                        ApplyDelta(targetObj, updateObj);
                }
            }
        }
    }

    private int FindMatchIndex(JsonArray array, JsonObject candidate)
    {
        if (candidate.TryGetPropertyValue("Id", out var idNode))
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is JsonObject obj && obj.TryGetPropertyValue("Id", out var targetId) && targetId.ToJsonString() == idNode.ToJsonString())
                {
                    return i;
                }
            }
        }

        var json = candidate.ToJsonString();

        for (int i = 0; i < array.Count; i++)
        {
            if (array[i].ToJsonString() == json)
                return i;
        }

        return -1;
    }

    private int FindPrimitiveIndex(JsonArray array, JsonNode value)
    {
        var json = value?.ToJsonString();

        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]?.ToJsonString() == json)
                return i;
        }

        return -1;
    }

    protected void Reset()
    {
        _oldJson = "";
        _newJson = "";
    }

    public void Dispose()
    {
        Reset();
    }

    private static string ToJson(object obj)
    {
        // Note: We should use the custom ZGWJsonSerializerSettings, handling specific types (eg. Geometry)
        return obj != null ? Newtonsoft.Json.JsonConvert.SerializeObject(obj, new ZGWJsonSerializerSettings()) : null;
    }
}
