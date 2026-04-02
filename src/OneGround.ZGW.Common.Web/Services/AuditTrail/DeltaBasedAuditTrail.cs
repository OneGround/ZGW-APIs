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
    private readonly IDbContextWithAuditTrail _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEntityUriService _uriService;

    private const int _snapshotInterval = 25; // TODO: May be add this in AuditTrailOptions? API could configire via appsettings or something like that.

    private AuditTrailOptions _options = new AuditTrailOptions();

    private string _oldJson;
    private string _newJson;

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

    public string Name => "Deltas";

    public bool Legacy => false;

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

        foreach (var auditsForResource in auditsGroupedByResource)
        {
            JsonNode? current = null;
            foreach (var audit in auditsForResource)
            {
                JsonDocument? oldVersion = current != null ? current.Deserialize<JsonDocument>() : default;
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
                            Id = audit.ResourceId.GetValueOrDefault(Guid.NewGuid()),
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
                            Nieuw = default!,
                            Oud = deletedVersion != null ? JsonSerializer.SerializeToNode(deletedVersion)?.ToString() : null,
                        }
                    );
                    current = null;
                    continue;
                }

                JsonDocument? newVersion = current != null ? current.Deserialize<JsonDocument>() : default;
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
        var requestedAuditVersie = await _context
            .AuditTrailDeltas.AsNoTracking()
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == hoofdobjectId)
            .Where(a => a.Id == audittrailId)
            .SingleOrDefaultAsync(cancellationToken);

        var audits = await _context
            .AuditTrailDeltas.AsNoTracking()
            .Where(a =>
                a.HoofdObjectId.HasValue
                && a.HoofdObjectId == hoofdobjectId
                && a.ResourceId.HasValue
                && a.ResourceId == requestedAuditVersie.ResourceId
                && a.Versie <= requestedAuditVersie.Versie
            )
            .OrderBy(a => a.Versie)
            .ToListAsync(cancellationToken);

        AuditTrailRegel auditTrailRegel = null;

        JsonNode? current = null;
        foreach (var audit in audits)
        {
            JsonDocument? oldVersion = current != null ? current.Deserialize<JsonDocument>() : default;
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

                auditTrailRegel = new AuditTrailRegel
                {
                    Id = audit.ResourceId.GetValueOrDefault(Guid.NewGuid()),
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
                    Nieuw = default!,
                    Oud = deletedVersion != null ? JsonSerializer.SerializeToNode(deletedVersion)?.ToString() : null,
                };

                current = null;
                continue;
            }
            else
            {
                // TODO: Handle retrieve of list, etc. For now, we just return null, as reconstructing those can be complex and may not be worth the effort.
                return null;
            }

            JsonDocument? newVersion = current != null ? current.Deserialize<JsonDocument>() : default;
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
        AuditActie auditActie,
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
            Actie = $"{auditActie}",
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
            switch (auditActie)
            {
                case AuditActie.create:
                {
                    audittrail.Versie = 1; //  await GetNextVersion(audittrail.ResourceId.Value); // TODO: test!!
                    audittrail.SnapshotJson = _newJson;
                    break;
                }

                case AuditActie.destroy:
                {
                    audittrail.Versie = await GetNextVersion(audittrail.ResourceId.Value);
                    audittrail.DeltaJson = _oldJson;
                    break;
                }

                case AuditActie.update:
                case AuditActie.partial_update:
                {
                    var original = JsonSerializer.Deserialize<JsonObject>(_oldJson);
                    var current = JsonSerializer.Deserialize<JsonObject>(_newJson);

                    // Genereer delta
                    var delta = AuditDeltaGenerator.GenerateDelta(original, current);

                    // No changes → Do not log
                    if (delta == null || delta.Count == 0)
                        return;

                    var versie = await GetNextVersion(audittrail.ResourceId.Value);

                    // Check if this is a snapshot version
                    bool isSnapshotVersion = versie % _snapshotInterval == 0;

                    audittrail.DeltaJson = isSnapshotVersion ? null : delta.ToJsonString();
                    audittrail.SnapshotJson = isSnapshotVersion ? _newJson : null;
                    audittrail.Versie = versie;
                    break;
                }

                case AuditActie.retrieve:
                {
                    // No delta for reads, only snapshot
                    audittrail.SnapshotJson = _newJson;
                    break;
                }
            }
        }

        await _context.AuditTrailDeltas.AddAsync(audittrail, cancellationToken);

        Reset();
    }

    private async Task<int> GetNextVersion(Guid resourceId)
    {
        var last = await _context.AuditTrailDeltas.Where(a => a.ResourceId == resourceId).MaxAsync(a => (int?)a.Versie) ?? 0;

        return last + 1;
    }

    private void ApplyDelta(JsonObject target, JsonObject delta)
    {
        foreach (var kv in delta)
        {
            var key = kv.Key;
            var value = kv.Value;

            if (value is JsonObject obj)
            {
                if (obj.ContainsKey("added") || obj.ContainsKey("removed") || obj.ContainsKey("updated"))
                {
                    var array = target[key] as JsonArray ?? new JsonArray();
                    ApplyArrayDelta(array, obj);
                    target[key] = array;
                }
                else
                {
                    var nested = target[key] as JsonObject ?? new JsonObject();
                    ApplyDelta(nested, obj);
                    target[key] = nested;
                }
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }

    private void ApplyArrayDelta(JsonArray target, JsonObject delta)
    {
        if (delta.TryGetPropertyValue("removed", out var removedNode))
        {
            foreach (var r in removedNode!.AsArray())
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
            foreach (var a in addedNode!.AsArray())
                target.Add(a!.DeepClone());
        }

        if (delta.TryGetPropertyValue("updated", out var updatedNode))
        {
            foreach (var u in updatedNode!.AsArray())
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
                if (
                    array[i] is JsonObject obj
                    && obj.TryGetPropertyValue("Id", out var targetId)
                    && targetId!.ToJsonString() == idNode!.ToJsonString()
                )
                {
                    return i;
                }
            }
        }

        var json = candidate.ToJsonString();

        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]!.ToJsonString() == json)
                return i;
        }

        return -1;
    }

    private int FindPrimitiveIndex(JsonArray array, JsonNode? value)
    {
        var json = value?.ToJsonString();

        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]?.ToJsonString() == json)
                return i;
        }

        return -1;
    }

    private static JsonObject CompareObjects(JsonObject original, JsonObject current)
    {
        var delta = new JsonObject();

        foreach (var kv in current)
        {
            original.TryGetPropertyValue(kv.Key, out var oldValue);

            if (kv.Value is JsonObject newObj && oldValue is JsonObject oldObj)
            {
                var child = CompareObjects(oldObj, newObj);
                if (child.Count > 0)
                    delta[kv.Key] = child;
            }
            else if (kv.Value is JsonArray newArr && oldValue is JsonArray oldArr)
            {
                var arrDelta = CompareArrays(oldArr, newArr);
                if (arrDelta.Count > 0)
                    delta[kv.Key] = arrDelta;
            }
            else
            {
                if (!JsonEquals(oldValue, kv.Value))
                    delta[kv.Key] = kv.Value!.DeepClone();
            }
        }

        return delta;
    }

    private static JsonObject CompareArrays(JsonArray original, JsonArray current)
    {
        var result = new JsonObject();

        var added = new JsonArray();
        var removed = new JsonArray();
        var updated = new JsonArray();

        var originalObjects = original.OfType<JsonObject>().ToDictionary(GetIdOrHash);
        var currentObjects = current.OfType<JsonObject>().ToDictionary(GetIdOrHash);

        // removed
        foreach (var key in originalObjects.Keys)
        {
            if (!currentObjects.ContainsKey(key))
            {
                removed.Add(originalObjects[key].DeepClone());
            }
        }

        // added
        foreach (var key in currentObjects.Keys)
        {
            if (!originalObjects.ContainsKey(key))
            {
                added.Add(currentObjects[key].DeepClone());
            }
        }

        // updated
        foreach (var key in originalObjects.Keys)
        {
            if (!currentObjects.ContainsKey(key))
                continue;

            var oldObj = originalObjects[key];
            var newObj = currentObjects[key];

            var delta = CompareObjects(oldObj, newObj);

            if (delta.Count > 0)
            {
                if (newObj.TryGetPropertyValue("Id", out var id))
                    delta["Id"] = id!.DeepClone();

                updated.Add(delta);
            }
        }

        if (added.Count > 0)
            result["added"] = added;

        if (removed.Count > 0)
            result["removed"] = removed;

        if (updated.Count > 0)
            result["updated"] = updated;

        return result;
    }

    private static string GetIdOrHash(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("Id", out var id))
            return id!.ToJsonString();

        return obj.ToJsonString();
    }

    private static bool JsonEquals(JsonNode? a, JsonNode? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;

        return a.ToJsonString() == b.ToJsonString();
    }

    private void Reset()
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
        return obj != null ? Newtonsoft.Json.JsonConvert.SerializeObject(obj, new ZGWJsonSerializerSettings()) : null;
    }
}
