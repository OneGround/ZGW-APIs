using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class GetResultaatTypeQueryHandler
    : CatalogiBaseHandler<GetResultaatTypeQueryHandler>,
        IRequestHandler<GetResultaatTypeQuery, QueryResult<ResultaatType>>
{
    private readonly ZtcDbContext _context;

    public GetResultaatTypeQueryHandler(
        ILogger<GetResultaatTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<ResultaatType>> Handle(GetResultaatTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ResultType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var resultType = await _context
            .ResultaatTypen.AsSplitQuery()
            .AsNoTracking()
            .Where(rsinFilter)
            .Include(s => s.ZaakType.Catalogus)
            .Include(s => s.BronDatumArchiefProcedure)
            .Include(z => z.ResultaatTypeBesluitTypen)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (resultType == null)
        {
            return new QueryResult<ResultaatType>(null, QueryStatus.NotFound);
        }

        // Resolve soft Resultaattype-BesluitType relations with the current version within geldigheid
        await ResolveZaakTypeBesluitTypeRelations(resultType, cancellationToken);

        return new QueryResult<ResultaatType>(resultType, QueryStatus.OK);
    }

    private async Task ResolveZaakTypeBesluitTypeRelations(ResultaatType resultType, CancellationToken cancellationToken)
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        List<ResultaatTypeBesluitType> resultaatTypeBesluitTypen = [];

        foreach (var resultaatTypeBesluitType in resultType.ResultaatTypeBesluitTypen)
        {
            var besluitTypenWithinGeldigheid = await _context
                .BesluitTypen.AsNoTracking()
                .Where(b => b.CatalogusId == resultType.ZaakType.CatalogusId)
                .Where(b => !b.Concept)
                .Where(b =>
                    b.Omschrijving == resultaatTypeBesluitType.BesluitTypeOmschrijving
                    && now >= b.BeginGeldigheid
                    && (b.EindeGeldigheid == null || now <= b.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            resultaatTypeBesluitTypen.AddRangeUnique(
                besluitTypenWithinGeldigheid.Select(b => new ResultaatTypeBesluitType
                {
                    ResultaatType = resultType,
                    BesluitTypeOmschrijving = b.Omschrijving,
                    Owner = b.Owner,
                    BesluitType = b,
                }),
                (x, y) => x.ResultaatType.Url == y.ResultaatType.Url && x.BesluitType.Url == y.BesluitType.Url
            );
        }

        resultType.ResultaatTypeBesluitTypen.Clear();
        resultType.ResultaatTypeBesluitTypen = resultaatTypeBesluitTypen;
    }
}

class GetResultaatTypeQuery : IRequest<QueryResult<ResultaatType>>
{
    public Guid Id { get; set; }
}
