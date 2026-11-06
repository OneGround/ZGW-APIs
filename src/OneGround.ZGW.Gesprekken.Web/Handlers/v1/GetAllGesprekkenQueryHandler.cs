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
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Models.v1;
using OneGround.ZGW.Gesprekken.Web.Paginering;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

class GetAllGesprekkenQueryHandler
    : GesprekkenBaseHandler<GetAllGesprekkenQueryHandler>,
        IRequestHandler<GetAllGesprekkenQuery, QueryResult<CursorPage<Gesprek>>>
{
    private readonly GrcDbContext _context;

    public GetAllGesprekkenQueryHandler(
        ILogger<GetAllGesprekkenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        GrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<CursorPage<Gesprek>>> Handle(GetAllGesprekkenQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        var query = _context.Gesprekken.AsNoTracking().Where(GetRsinFilterPredicate<Gesprek>()).Where(GetFilterPredicate(filter));

        if (filter.GespreksdeelnamePartij != null)
        {
            query = query.Where(g => g.Deelnames.Any(d => d.Partij == filter.GespreksdeelnamePartij));
        }

        if (filter.GespreksdeelnameActor != null)
        {
            query = query.Where(g => g.Deelnames.Any(d => d.Actor == filter.GespreksdeelnameActor));
        }

        if (filter.Gespreksdeelname.HasValue)
        {
            query = query.Where(g => g.Deelnames.Any(d => d.Id == filter.Gespreksdeelname));
        }

        if (filter.GespreksdeelnameType.HasValue)
        {
            query = query.Where(g => g.Deelnames.Any(d => d.Type == filter.GespreksdeelnameType));
        }

        var page = await GesprekPaginering.PageAsync(query, request.After, request.PageSize, cancellationToken);

        return new QueryResult<CursorPage<Gesprek>>(page, QueryStatus.OK);
    }

    private static Expression<Func<Gesprek, bool>> GetFilterPredicate(GetAllGesprekkenFilter filter)
    {
        return g =>
            (filter.Referentie == null || g.Referentie == filter.Referentie)
            && (filter.AanleidingZaak == null || g.AanleidingZaak == filter.AanleidingZaak)
            && (filter.AanleidingPlan == null || g.AanleidingPlan == filter.AanleidingPlan)
            && (filter.AanvangsmomentGesprekGte == null || g.AanvangsmomentGesprek >= filter.AanvangsmomentGesprekGte)
            && (filter.AanvangsmomentGesprekLte == null || g.AanvangsmomentGesprek <= filter.AanvangsmomentGesprekLte);
    }
}

class GetAllGesprekkenQuery : IRequest<QueryResult<CursorPage<Gesprek>>>
{
    public GetAllGesprekkenFilter Filter { get; set; }
    public GesprekCursor After { get; set; }
    public int PageSize { get; set; }
}
