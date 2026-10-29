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
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1._7;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._7;

class GetAllZaakNotitiesQueryHandler
    : ZakenBaseHandler<GetAllZaakNotitiesQueryHandler>,
        IRequestHandler<GetAllZaakNotitiesQuery, QueryResult<PagedResult<ZaakNotitie>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakNotitiesQueryHandler(
        ILogger<GetAllZaakNotitiesQueryHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<ZaakNotitie>>> Handle(GetAllZaakNotitiesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakNotities....");

        var filter = GetZaakNotitieFilterPredicate(request.GetAllZaakNotitiesFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakNotitie>();

        var query = _context.ZaakNotities.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { ZaakNotitie = r, Authorisatie = a })
                .Where(r => (int)r.ZaakNotitie.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakNotitie);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(z => z.Zaak)
            .OrderBy(z => z.CreationTime)
            .ThenBy(z => z.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakNotitie> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakNotitie>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakNotitie, bool>> GetZaakNotitieFilterPredicate(GetAllZaakNotitiesFilter filter)
    {
        // Note: A notitie that is never modified has the wijzigingsdatum equal to the aanmaakdatum
        // Note: An exact aanmaakdatum/wijzigingsdatum matches the day or second that was given (stored timestamps have microseconds)
        var gerelateerdAanId = filter.GerelateerdAan == null ? (Guid?)null : UriHelper.GetResourceId(filter.GerelateerdAan);
        var aangemaaktDoorLike = LikePattern(filter.AangemaaktDoor__icontains);
        var onderwerpLike = LikePattern(filter.Onderwerp__icontains);
        var tekstLike = LikePattern(filter.Tekst__icontains);

        return z =>
            (filter.AangemaaktDoor == null || z.AangemaaktDoor == filter.AangemaaktDoor)
            && (aangemaaktDoorLike == null || EF.Functions.ILike(z.AangemaaktDoor, aangemaaktDoorLike, LikeEscape))
            && (filter.Aanmaakdatum == null || (z.CreationTime >= filter.Aanmaakdatum.From && z.CreationTime < filter.Aanmaakdatum.Until))
            && (filter.Aanmaakdatum__gt == null || z.CreationTime > filter.Aanmaakdatum__gt)
            && (filter.Aanmaakdatum__gte == null || z.CreationTime >= filter.Aanmaakdatum__gte)
            && (filter.Aanmaakdatum__lt == null || z.CreationTime < filter.Aanmaakdatum__lt)
            && (filter.Aanmaakdatum__lte == null || z.CreationTime <= filter.Aanmaakdatum__lte)
            && (gerelateerdAanId == null || z.ZaakId == gerelateerdAanId)
            && (filter.NotitieType == null || z.NotitieType == filter.NotitieType)
            && (filter.Onderwerp == null || z.Onderwerp == filter.Onderwerp)
            && (onderwerpLike == null || EF.Functions.ILike(z.Onderwerp, onderwerpLike, LikeEscape))
            && (filter.Status == null || z.Status == filter.Status)
            && (filter.Tekst == null || z.Tekst == filter.Tekst)
            && (tekstLike == null || EF.Functions.ILike(z.Tekst, tekstLike, LikeEscape))
            && (
                filter.Wijzigingsdatum == null
                || (
                    (z.ModificationTime ?? z.CreationTime) >= filter.Wijzigingsdatum.From
                    && (z.ModificationTime ?? z.CreationTime) < filter.Wijzigingsdatum.Until
                )
            )
            && (filter.Wijzigingsdatum__gt == null || (z.ModificationTime ?? z.CreationTime) > filter.Wijzigingsdatum__gt)
            && (filter.Wijzigingsdatum__gte == null || (z.ModificationTime ?? z.CreationTime) >= filter.Wijzigingsdatum__gte)
            && (filter.Wijzigingsdatum__lt == null || (z.ModificationTime ?? z.CreationTime) < filter.Wijzigingsdatum__lt)
            && (filter.Wijzigingsdatum__lte == null || (z.ModificationTime ?? z.CreationTime) <= filter.Wijzigingsdatum__lte);
    }

    private const string LikeEscape = "\\";

    // Note: The wildcards of the given text itself are escaped, so it is always matched as plain text
    private static string LikePattern(string contains) =>
        contains == null ? null : "%" + contains.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}

class GetAllZaakNotitiesQuery : IRequest<QueryResult<PagedResult<ZaakNotitie>>>
{
    public GetAllZaakNotitiesFilter GetAllZaakNotitiesFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
