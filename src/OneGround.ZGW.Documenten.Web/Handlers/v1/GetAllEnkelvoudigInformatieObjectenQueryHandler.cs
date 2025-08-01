using System;
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
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class GetAllEnkelvoudigInformatieObjectenQueryHandler
    : DocumentenBaseHandler<GetAllEnkelvoudigInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllEnkelvoudigInformatieObjectenQuery, QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    private readonly DrcDbContext _context;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    public GetAllEnkelvoudigInformatieObjectenQueryHandler(
        ILogger<GetAllEnkelvoudigInformatieObjectenQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<EnkelvoudigInformatieObject>>> Handle(
        GetAllEnkelvoudigInformatieObjectenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all EnkelvoudigInformatieObjecten....");

        var filter = GetEnkelvoudigInformatieObjectFilterPredicate(request.GetAllEnkelvoudigInformatieObjectenFilter);
        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var query = _context.EnkelvoudigInformatieObjecten.AsNoTracking().Where(rsinFilter).Where(filter);

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
                    o => o.InformatieObjectType,
                    i => i.InformatieObjectType,
                    (i, a) => new { InformatieObject = i, Authorisatie = a }
                )
                .Where(i => i.InformatieObject.LatestEnkelvoudigInformatieObjectVersie.Owner == _rsin)
                .Where(i =>
                    (int)i.InformatieObject.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding
                    <= i.Authorisatie.MaximumVertrouwelijkheidAanduiding
                )
                .Select(i => i.InformatieObject);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(e => e.EnkelvoudigInformatieObjectVersies)
            .Include(e => e.GebruiksRechten)
            .OrderBy(e => e.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<EnkelvoudigInformatieObject> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<EnkelvoudigInformatieObject>>(result, QueryStatus.OK);
    }

    private static Expression<Func<EnkelvoudigInformatieObject, bool>> GetEnkelvoudigInformatieObjectFilterPredicate(
        GetAllEnkelvoudigInformatieObjectenFilter filter
    )
    {
        return e =>
            (filter.Bronorganisatie == null || e.EnkelvoudigInformatieObjectVersies.Any(e => e.Bronorganisatie == filter.Bronorganisatie))
            && (filter.Identificatie == null || e.EnkelvoudigInformatieObjectVersies.Any(e => e.Identificatie == filter.Identificatie));
    }
}

class GetAllEnkelvoudigInformatieObjectenQuery : IRequest<QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    public GetAllEnkelvoudigInformatieObjectenFilter GetAllEnkelvoudigInformatieObjectenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
