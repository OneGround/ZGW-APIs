using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class GetAllZaakTypeInformatieObjectTypenQueryHandler
    : CatalogiBaseHandler<GetAllZaakTypeInformatieObjectTypenQueryHandler>,
        IRequestHandler<GetAllZaakTypeInformatieObjectTypenQuery, QueryResult<PagedResult<ZaakTypeInformatieObjectType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllZaakTypeInformatieObjectTypenQueryHandler(
        ILogger<GetAllZaakTypeInformatieObjectTypenQueryHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<ZaakTypeInformatieObjectType>>> Handle(
        GetAllZaakTypeInformatieObjectTypenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all ZaakTypeInformatieObjectTypen....");

        var filter = GetZaakTypeInformatieObjectTypen(request.GetAllZaakTypenFilter);

        var rsinZiotFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);
        var rsinIotFilter = GetRsinFilterPredicate<InformatieObjectType>(b => b.Catalogus.Owner == _rsin && b.Catalogus.Id == b.CatalogusId);

        // Note: Be sure we have only resolvable informatieobjectypen within each page (soft ZaakType-InformatieObjectTypen relations)

        var datumGeldigheid = DateOnly.FromDateTime(DateTime.Now);
        var informatieObjectTypenWithinGeldigheid = _context
            .InformatieObjectTypen.Where(rsinIotFilter)
            .Where(i => !i.Concept && datumGeldigheid >= i.BeginGeldigheid && (i.EindeGeldigheid == null || datumGeldigheid <= i.EindeGeldigheid));

        var query = _context
            .ZaakTypeInformatieObjectTypen.AsNoTracking()
            .Include(z => z.ZaakType)
            .Include(s => s.StatusType)
            .Where(rsinZiotFilter)
            .Join(
                informatieObjectTypenWithinGeldigheid,
                k => k.ZaakType.CatalogusId.ToString() + k.InformatieObjectTypeOmschrijving,
                i => i.CatalogusId.ToString() + i.Omschrijving,
                (z, i) => new { ZaakTypeInformatieObjectType = z, InformatieObjectType = i }
            )
            .Select(z => new ZaakTypeInformatieObjectType
            {
                Id = z.ZaakTypeInformatieObjectType.Id,
                InformatieObjectType = z.InformatieObjectType, // Note: Resolve soft relation here!
                ZaakType = z.ZaakTypeInformatieObjectType.ZaakType,
                Richting = z.ZaakTypeInformatieObjectType.Richting,
                VolgNummer = z.ZaakTypeInformatieObjectType.VolgNummer,
                StatusType = z.ZaakTypeInformatieObjectType.StatusType,
            })
            .Where(filter) // Iot is now resolved so we can filter on it
            .Distinct()
            .OrderBy(z => z.Id);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakTypeInformatieObjectType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakTypeInformatieObjectType>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakTypeInformatieObjectType, bool>> GetZaakTypeInformatieObjectTypen(GetAllZaakTypeInformatieObjectTypenFilter filter)
    {
        return z =>
            (
                filter.Status == ConceptStatus.concept && z.ZaakType.Concept == true
                || filter.Status == ConceptStatus.definitief && z.ZaakType.Concept == false
                || filter.Status == ConceptStatus.alles
            )
            && (filter.ZaakType == null || z.ZaakType.Id == _uriService.GetId(filter.ZaakType))
            && (filter.InformatieObjectType == null || z.InformatieObjectType.Id == _uriService.GetId(filter.InformatieObjectType))
            && (filter.Richting == null || z.Richting == filter.Richting);
    }
}

class GetAllZaakTypeInformatieObjectTypenQuery : IRequest<QueryResult<PagedResult<ZaakTypeInformatieObjectType>>>
{
    public GetAllZaakTypeInformatieObjectTypenFilter GetAllZaakTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
