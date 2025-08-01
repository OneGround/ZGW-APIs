using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class GetAllGebruiksRechtenQueryHandler
    : DocumentenBaseHandler<GetAllGebruiksRechtenQueryHandler>,
        IRequestHandler<GetAllGebruiksRechtenQuery, QueryResult<IList<GebruiksRecht>>>
{
    private readonly DrcDbContext _context;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    public GetAllGebruiksRechtenQueryHandler(
        ILogger<GetAllGebruiksRechtenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IInformatieObjectAuthorizationTempTableService informatieObjectAuthorizationTempTableService,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _informatieObjectAuthorizationTempTableService = informatieObjectAuthorizationTempTableService;
    }

    public async Task<QueryResult<IList<GebruiksRecht>>> Handle(GetAllGebruiksRechtenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all GebruiksRechten....");

        var filter = GetGebruiksRechtFilterPredicate(request.GetAllGebruiksRechtenFilter);
        var rsinFilter = GetRsinFilterPredicate<GebruiksRecht>(o => o.InformatieObject.Owner == _rsin);

        var query = _context.GebruiksRechten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _informatieObjectAuthorizationTempTableService.InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            query = query
                .Join(
                    _context.TempInformatieObjectAuthorization,
                    o => o.InformatieObject.InformatieObjectType,
                    i => i.InformatieObjectType,
                    (i, a) => new { InformatieObject = i, Authorisatie = a }
                )
                .Join(
                    _context.EnkelvoudigInformatieObjectVersies.AsNoTracking(),
                    ea => ea.InformatieObject.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId,
                    e0 => e0.Id,
                    (i, v) =>
                        new
                        {
                            i.InformatieObject,
                            InformatieObjectVersie = v,
                            i.Authorisatie,
                        }
                )
                .Where(i => i.InformatieObject.InformatieObject.LatestEnkelvoudigInformatieObjectVersie.Owner == _rsin)
                .Where(i => (int)i.InformatieObjectVersie.Vertrouwelijkheidaanduiding <= i.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(i => i.InformatieObject);
        }

        var result = await query.Include(z => z.InformatieObject).OrderBy(e => e.Id).AsSplitQuery().ToListAsync(cancellationToken);

        return new QueryResult<IList<GebruiksRecht>>(result, QueryStatus.OK);
    }

    private Expression<Func<GebruiksRecht, bool>> GetGebruiksRechtFilterPredicate(GetAllGebruiksRechtenFilter filter)
    {
        return z =>
            (filter.InformatieObject == null || z.InformatieObject.Id == _uriService.GetId(filter.InformatieObject))
            && (filter.Startdatum__gt == null || z.Startdatum > filter.Startdatum__gt)
            && (filter.Startdatum__gte == null || z.Startdatum >= filter.Startdatum__gte)
            && (filter.Startdatum__lt == null || z.Startdatum < filter.Startdatum__lt)
            && (filter.Startdatum__lte == null || z.Startdatum <= filter.Startdatum__lte)
            && (filter.Einddatum__gt == null || (z.Einddatum != null && z.Einddatum > filter.Einddatum__gt))
            && (filter.Einddatum__gte == null || (z.Einddatum != null && z.Einddatum >= filter.Einddatum__gte))
            && (filter.Einddatum__lt == null || (z.Einddatum != null && z.Einddatum < filter.Einddatum__lt))
            && (filter.Einddatum__lte == null || (z.Einddatum != null && z.Einddatum <= filter.Einddatum__lte));
    }
}

class GetAllGebruiksRechtenQuery : IRequest<QueryResult<IList<GebruiksRecht>>>
{
    public GetAllGebruiksRechtenFilter GetAllGebruiksRechtenFilter { get; internal set; }
}
