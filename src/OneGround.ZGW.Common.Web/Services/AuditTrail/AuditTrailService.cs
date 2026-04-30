using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

public class AuditTrailService : AuditTrailServiceBase
{
    public AuditTrailService(IDbContextWithAuditTrail context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IEntityUriService uriService)
        : base(context, mapper, httpContextAccessor, uriService) { }

    protected override async Task WriteAsync(
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

    protected override async Task WriteAsync(
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

    protected override async Task<IEnumerable<AuditTrailRegel>> ReadAsync(Guid hoofdobjectId, CancellationToken cancellationToken = default)
    {
        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId.Value == hoofdobjectId)
            .OrderBy(a => a.AanmaakDatum)
            .ToListAsync(cancellationToken);

        return result;
    }

    protected override async Task<AuditTrailRegel> ReadAsync(Guid hoofdobjectId, Guid audittrailId, CancellationToken cancellationToken = default)
    {
        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == audittrailId && a.HoofdObjectId == hoofdobjectId, cancellationToken);

        return result;
    }
}
