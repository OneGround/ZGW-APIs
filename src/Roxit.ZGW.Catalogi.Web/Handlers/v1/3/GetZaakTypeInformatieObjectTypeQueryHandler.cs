using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

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
            .ThenInclude(z => z.Catalogus)
            .Include(s => s.StatusType)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakTypeInformatieObjectType == null)
        {
            return new QueryResult<ZaakTypeInformatieObjectType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ZaakTypeInformatieObjectType>(zaakTypeInformatieObjectType, QueryStatus.OK);
    }
}

class GetZaakTypeInformatieObjectTypeQuery : IRequest<QueryResult<ZaakTypeInformatieObjectType>>
{
    public Guid Id { get; set; }
}
