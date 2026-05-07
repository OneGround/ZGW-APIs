using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public abstract class AuditTrailServiceBase : IAuditTrailService
{
    // Generic settings
    public const string MaskingEnabled = "MaskingEnabled";

    protected readonly IDbContextWithAuditTrail _context;
    protected readonly IMapper _mapper;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly IEntityUriService _uriService;

    protected AuditTrailOptions _options = new AuditTrailOptions();

    protected string _oldJson;
    protected string _newJson;

    public AuditTrailServiceBase(
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

    public virtual void SetOptions(AuditTrailOptions options)
    {
        _options = options;
    }

    public virtual void SetOld<TDto>(IBaseEntity entity)
    {
        var oldDto = _mapper.Map<TDto>(entity);

        if (IsMaskingEabled())
        {
            oldDto = ApplyMaskingFields(oldDto);
        }

        _oldJson = ToJson(oldDto);
    }

    public virtual void SetNew<TDto>(IBaseEntity entity)
    {
        var newDto = _mapper.Map<TDto>(entity);

        if (IsMaskingEabled())
        {
            newDto = ApplyMaskingFields(newDto);
        }

        _newJson = ToJson(newDto);
    }

    public virtual Task CreatedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
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

    public virtual Task GetAsync(
        IBaseEntity entity,
        IUrlEntity subEntity,
        string overruleActieWeergave = null,
        CancellationToken cancellationToken = default
    )
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

    public virtual Task GetListAsync(int count, int totalCount, int page, string filter = null, CancellationToken cancellationToken = default)
    {
        return totalCount == 0
            ? WriteAsync(
                AuditActie.retrieve,
                filter == null ? "Lijst van objecten gelezen" : filter + " gefilterde lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: filter == null ? "Lijst bevat geen objecten" : filter + " gefilterde lijst bevat geen objecten",
                cancellationToken: cancellationToken
            )
            : WriteAsync(
                AuditActie.retrieve,
                filter == null ? "Lijst van objecten gelezen" : filter + " gefilterde lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: filter == null
                    ? $"Lijst van {count} van {totalCount} objecten gelezen (pagina {page})"
                    : $"{filter} gefilterde lijst van {count} van {totalCount} objecten gelezen (pagina {page})",
                cancellationToken: cancellationToken
            );
    }

    public virtual Task GetListAsync(int totalCount, string filter = null, CancellationToken cancellationToken = default)
    {
        return totalCount == 0
            ? WriteAsync(
                AuditActie.retrieve,
                filter == null ? "Lijst van objecten gelezen" : filter + " gefilterde lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: filter == null ? "Lijst bevat geen objecten" : filter + " gefilterde lijst bevat geen objecten",
                cancellationToken: cancellationToken
            )
            : WriteAsync(
                AuditActie.retrieve,
                filter == null ? "Lijst van objecten gelezen" : filter + " gefilterde lijst van objecten gelezen",
                "(lijst van)",
                "(lijst van)",
                HttpStatusCode.OK,
                toelichting: filter == null
                    ? $"Lijst van {totalCount} objecten gelezen"
                    : $"{filter} gefilterde lijst van {totalCount} objecten gelezen",
                cancellationToken: cancellationToken
            );
    }

    public virtual Task DestroyedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
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

    public virtual Task DestroyedAsync(IUrlEntity entity, string toelichting, CancellationToken cancellationToken = default)
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

    public virtual Task UpdatedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
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

    public virtual Task PatchedAsync(IBaseEntity entity, IUrlEntity subEntity, CancellationToken cancellationToken = default)
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

    public virtual Task PatchedAsync(IBaseEntity entity, IUrlEntity subEntity, string toelichting, CancellationToken cancellationToken = default)
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

    public virtual Task<IEnumerable<AuditTrailRegel>> GetAuditTrailEntriesAsync(Guid hoofdobjectId, CancellationToken cancellationToken = default)
    {
        return ReadAsync(hoofdobjectId, cancellationToken);
    }

    public virtual Task<AuditTrailRegel> GetAuditTrailEntryByIdAsync(
        Guid hoofdobjectId,
        Guid audittrailId,
        CancellationToken cancellationToken = default
    )
    {
        return ReadAsync(hoofdobjectId, audittrailId, cancellationToken);
    }

    protected abstract Task WriteAsync(
        AuditActie auditActie,
        string actieWeergave,
        string hoofdobject,
        string resourceUrl,
        HttpStatusCode resultaat,
        string resourceWeergave = "",
        string toelichting = "",
        CancellationToken cancellationToken = default
    );

    protected abstract Task WriteAsync(
        AuditActie auditActie,
        string actieWeergave,
        IBaseEntity hoofdobject,
        string resourceUrl,
        HttpStatusCode resultaat,
        string resourceWeergave = "",
        string toelichting = "",
        bool dontWriteEntity = false,
        CancellationToken cancellationToken = default
    );

    protected abstract Task<IEnumerable<AuditTrailRegel>> ReadAsync(Guid hoofdobjectId, CancellationToken cancellationToken);

    protected abstract Task<AuditTrailRegel> ReadAsync(Guid hoofdobjectId, Guid audittrailId, CancellationToken cancellationToken);

    protected virtual void Reset()
    {
        _oldJson = "";
        _newJson = "";
    }

    public void Dispose()
    {
        Reset();
    }

    private static TDto ApplyMaskingFields<TDto>(TDto dto)
    {
        if (dto == null)
            return dto;

        ApplyMaskingFieldsRecursive(dto, dto.GetType(), new HashSet<object>());
        return dto;
    }

    private static void ApplyMaskingFieldsRecursive(object obj, Type type, HashSet<object> visited)
    {
        if (obj == null || !visited.Add(obj)) // Note: 'visited' prevents infinite recursion for circular references!
            return;

        foreach (var propertyInfo in type.GetProperties())
        {
            if (propertyInfo.GetIndexParameters().Length > 0)
            {
                continue;
            }

            // Ignore complex (external) datatypes. For now ignore complex type Geometry only
            if (propertyInfo.PropertyType == typeof(Geometry))
            {
                continue;
            }

            if (propertyInfo.GetCustomAttribute<AuditMaskFieldAttribute>() != null)
            {
                if (propertyInfo.CanRead && propertyInfo.CanWrite && propertyInfo.PropertyType == typeof(string))
                {
                    propertyInfo.SetValue(obj, "******");
                }
            }
            else if (
                propertyInfo.CanRead
                && propertyInfo.PropertyType.IsClass
                && propertyInfo.PropertyType != typeof(string)
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyInfo.PropertyType)
            )
            {
                var childObj = propertyInfo.GetValue(obj);
                if (childObj != null)
                {
                    ApplyMaskingFieldsRecursive(childObj, propertyInfo.PropertyType, visited);
                }
            }
            else if (
                propertyInfo.CanRead
                && propertyInfo.PropertyType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyInfo.PropertyType)
            )
            {
                var collection = propertyInfo.GetValue(obj) as System.Collections.IEnumerable;
                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        if (item != null && item.GetType().IsClass && item is not string)
                        {
                            ApplyMaskingFieldsRecursive(item, item.GetType(), visited);
                        }
                    }
                }
            }
        }
    }

    private static string ToJson(object obj)
    {
        return obj != null ? JsonConvert.SerializeObject(obj, new ZGWJsonSerializerSettings()) : null;
    }

    private bool IsMaskingEabled()
    {
        if (
            _options.Properties != null
            && _options.Properties.TryGetValue(MaskingEnabled, out var value)
            && bool.TryParse($"{value}", out var maskingEnabled)
            && maskingEnabled
        )
        {
            return true;
        }
        return false;
    }
}
