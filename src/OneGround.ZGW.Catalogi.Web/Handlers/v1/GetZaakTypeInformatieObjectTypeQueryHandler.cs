using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class GetZaakTypeInformatieObjectTypeQueryHandler
    : CatalogiBaseHandler<GetZaakTypeInformatieObjectTypeQueryHandler>,
        IRequestHandler<GetZaakTypeInformatieObjectTypeQuery, QueryResult<ZaakTypeInformatieObjectType>>
{
    private readonly ZtcDbContext _context;

    public GetZaakTypeInformatieObjectTypeQueryHandler(
        ILogger<GetZaakTypeInformatieObjectTypeQueryHandler> logger,
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

    public async Task<QueryResult<ZaakTypeInformatieObjectType>> Handle(
        GetZaakTypeInformatieObjectTypeQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get ZaakTypeInformatieObjectType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var zaakTypeInformatieObjectType = await _context
            .ZaakTypeInformatieObjectTypen.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.ZaakType)
            .Include(s => s.StatusType)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakTypeInformatieObjectType == null)
        {
            return new QueryResult<ZaakTypeInformatieObjectType>(null, QueryStatus.NotFound);
        }

        // And finally resolve NotMapped member InformatieObjectType (soft relations zaaktype->informatieobjecttype within geldigheid)
        if (!await ResolveZaakTypeInformatieObjectTypeRelation(zaakTypeInformatieObjectType, cancellationToken))
        {
            return new QueryResult<ZaakTypeInformatieObjectType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ZaakTypeInformatieObjectType>(zaakTypeInformatieObjectType, QueryStatus.OK);
    }

    private async Task<bool> ResolveZaakTypeInformatieObjectTypeRelation(
        ZaakTypeInformatieObjectType zaakTypeInformatieObjectType,
        CancellationToken cancellationToken
    )
    {
        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        var informatieObjectTypenWithinGeldigheid = await _context
            .InformatieObjectTypen.AsNoTracking()
            .Where(i => i.CatalogusId == zaakTypeInformatieObjectType.ZaakType.CatalogusId)
            .Where(i => !i.Concept)
            .Where(i =>
                i.Omschrijving == zaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving
                && now >= i.BeginGeldigheid
                && (i.EindeGeldigheid == null || now <= i.EindeGeldigheid)
            )
            .ToListAsync(cancellationToken);

        // Note: Due overlaps (the same InformatieObjectType.omschrijving within geldigheid) we could have more results, so take the latest (BeginGeldigheid)
        zaakTypeInformatieObjectType.InformatieObjectType = informatieObjectTypenWithinGeldigheid.OrderBy(i => i.BeginGeldigheid).LastOrDefault();

        bool found = zaakTypeInformatieObjectType.InformatieObjectType != null;

        return found;
    }
}

class GetZaakTypeInformatieObjectTypeQuery : IRequest<QueryResult<ZaakTypeInformatieObjectType>>
{
    public Guid Id { get; set; }
}
