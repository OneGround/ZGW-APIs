using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.DataAccess.AuditTrail;

namespace Roxit.ZGW.Common.Web.Services.AuditTrail;

public class AuditTrailService : IAuditTrailService
{
    private readonly IDbContextWithAuditTrail _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEntityUriService _uriService;

    private AuditTrailOptions _options = new AuditTrailOptions();

    private string _oldJson;
    private string _newJson;

    public AuditTrailService(IDbContextWithAuditTrail context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IEntityUriService uriService)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _uriService = uriService;
    }

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

    public Task GetListAsync(int totalCount, CancellationToken cancellationToken)
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

        var audittrail = new AuditTrailRegel
        {
            AanmaakDatum = DateTime.UtcNow,
            Oud = null,
            Nieuw = null,
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
            HoofdObjectId = null,
        };

        await _context.AuditTrailRegels.AddAsync(audittrail, cancellationToken);

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

        var httpContext = _httpContextAccessor.HttpContext;

        var audittrail = new AuditTrailRegel
        {
            AanmaakDatum = DateTime.UtcNow,
            Oud = dontWriteEntity ? null : _oldJson,
            Nieuw = dontWriteEntity ? null : _newJson,
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
        };

        await _context.AuditTrailRegels.AddAsync(audittrail, cancellationToken);

        Reset();
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
        return obj != null ? JsonConvert.SerializeObject(obj, new ZGWJsonSerializerSettings()) : null;
    }
}
