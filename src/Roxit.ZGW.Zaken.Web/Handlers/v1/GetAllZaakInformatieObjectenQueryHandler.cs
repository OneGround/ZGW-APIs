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
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Models.v1;
using Roxit.ZGW.Zaken.Web.Services;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class GetAllZaakInformatieObjectenQueryHandler
    : ZakenBaseHandler<GetAllZaakInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllZaakInformatieObjectenQuery, QueryResult<IList<ZaakInformatieObject>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakInformatieObjectenQueryHandler(
        ILogger<GetAllZaakInformatieObjectenQueryHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<IList<ZaakInformatieObject>>> Handle(GetAllZaakInformatieObjectenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakInformatieObjecten....");

        if (
            string.IsNullOrEmpty(request.GetAllZaakInformatieObjectenFilter.InformatieObject)
            && string.IsNullOrEmpty(request.GetAllZaakInformatieObjectenFilter.Zaak)
        )
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Either zaak or informatieobject should be specified");

            return new QueryResult<IList<ZaakInformatieObject>>(null, QueryStatus.ValidationError, error);
        }

        var filter = GetZaakInformatieObjectFilterPredicate(request.GetAllZaakInformatieObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakInformatieObject>();

        var query = _context.ZaakInformatieObjecten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(
                    _context.TempZaakAuthorization,
                    o => o.Zaak.Zaaktype,
                    i => i.ZaakType,
                    (r, a) => new { ZaakInformatieObject = r, Authorisatie = a }
                )
                .Where(r => (int)r.ZaakInformatieObject.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakInformatieObject);
        }

        var result = await query.Include(z => z.Zaak).ToListAsync(cancellationToken);

        return new QueryResult<IList<ZaakInformatieObject>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakInformatieObject, bool>> GetZaakInformatieObjectFilterPredicate(GetAllZaakInformatieObjectenFilter filter)
    {
        return z =>
            (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak))
            && (filter.InformatieObject == null || z.InformatieObject == filter.InformatieObject);
    }
}

class GetAllZaakInformatieObjectenQuery : IRequest<QueryResult<IList<ZaakInformatieObject>>>
{
    public GetAllZaakInformatieObjectenFilter GetAllZaakInformatieObjectenFilter { get; internal set; }
}
