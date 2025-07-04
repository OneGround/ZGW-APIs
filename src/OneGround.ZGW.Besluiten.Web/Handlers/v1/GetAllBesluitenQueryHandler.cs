using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.DataModel.Authorization;
using OneGround.ZGW.Besluiten.Web.Models.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class GetAllBesluitenQueryHandler
    : BesluitenBaseHandler<GetAllBesluitenQueryHandler>,
        IRequestHandler<GetAllBesluitenQuery, QueryResult<PagedResult<Besluit>>>
{
    private readonly BrcDbContext _context;
    private readonly ITemporaryTableProvider _temporaryTableProvider;

    public GetAllBesluitenQueryHandler(
        ILogger<GetAllBesluitenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ITemporaryTableProvider temporaryTableProvider
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _temporaryTableProvider = temporaryTableProvider;
    }

    public async Task<QueryResult<PagedResult<Besluit>>> Handle(GetAllBesluitenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Besluiten....");

        var filter = GetBesluitFilterPredicate(request.GetAllBesluitenFilter);
        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var query = _context.Besluiten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await InsertBesluitAuthorizationsToTempTableAsync(cancellationToken);

            query = query
                .Join(_context.TempBesluitAuthorization, o => o.BesluitType, i => i.BesluitType, (b, a) => new { Besluit = b, Authorisatie = a })
                .Select(b => b.Besluit);
        }

        var totalCount = await query.CountAsync(filter, cancellationToken);

        var pagedResult = await query
            .OrderBy(b => b.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Besluit> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Besluit>>(result, QueryStatus.OK);
    }

    private static Expression<Func<Besluit, bool>> GetBesluitFilterPredicate(GetAllBesluitenFilter filter)
    {
        return z =>
            (filter.Identificatie == null || z.Identificatie == filter.Identificatie)
            && (filter.VerantwoordelijkeOrganisatie == null || z.VerantwoordelijkeOrganisatie == filter.VerantwoordelijkeOrganisatie)
            && (filter.BesluitType == null || z.BesluitType == filter.BesluitType)
            && (filter.Zaak == null || z.Zaak == filter.Zaak);
    }

    private async Task InsertBesluitAuthorizationsToTempTableAsync(CancellationToken cancellationToken)
    {
        await CreateTempTableBesluitAuthorizationAsync(cancellationToken);

        var besluitAuthorizations = _authorizationContext
            .Authorization.Authorizations.Where(permission => permission.BesluitType != null)
            .GroupBy(permission => permission.BesluitType)
            .Select(g => new TempBesluitAuthorization { BesluitType = g.Key });

        await _context.TempBesluitAuthorization.AddRangeAsync(besluitAuthorizations, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateTempTableBesluitAuthorizationAsync(CancellationToken cancellationToken)
    {
        const string sql = $"""
            CREATE TEMPORARY TABLE "{nameof(TempBesluitAuthorization)}"
            (
               "{nameof(TempBesluitAuthorization.BesluitType)}" text NOT NULL,
               PRIMARY KEY ("{nameof(TempBesluitAuthorization.BesluitType)}")
            )
            """;
        await _temporaryTableProvider.CreateAsync(_context, sql, cancellationToken);
    }
}

class GetAllBesluitenQuery : IRequest<QueryResult<PagedResult<Besluit>>>
{
    public GetAllBesluitenFilter GetAllBesluitenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
