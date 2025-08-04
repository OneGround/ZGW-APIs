using System;
using System.Linq;
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
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

class GetEnkelvoudigInformatieObjectQueryHandler
    : DocumentenBaseHandler<GetEnkelvoudigInformatieObjectQueryHandler>,
        IRequestHandler<GetEnkelvoudigInformatieObjectQuery, QueryResult<EnkelvoudigInformatieObject>>
{
    private readonly DrcDbContext _context;

    public GetEnkelvoudigInformatieObjectQueryHandler(
        ILogger<GetEnkelvoudigInformatieObjectQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
    }

    public async Task<QueryResult<EnkelvoudigInformatieObject>> Handle(
        GetEnkelvoudigInformatieObjectQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get EnkelvoudigInformatieObject {Id}....", request.Id);

        // Note: The object contains metadata about the document and the download link (content) to the binary data.
        // By default, this returns the latest version of the (SINGLE) INFORMATION OBJECT. Specific versions can
        // be requested by means of query string parameters.

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var enkelvoudiginformatieobject = await _context
            .EnkelvoudigInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .Include(e => e.EnkelvoudigInformatieObjectVersies)
            .ThenInclude(e => e.BestandsDelen)
            .Include(e => e.GebruiksRechten)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enkelvoudiginformatieobject == null)
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.NotFound);
        }

        EnkelvoudigInformatieObjectVersie requestedVersie;

        var filter = request.GetEnkelvoudigInformatieObjectFilter;

        if (!filter.Versie.HasValue && !filter.RegistratieOp.HasValue)
        {
            // No filter specified so get latest version of the EnkelvoudigInformatieObject
            requestedVersie = enkelvoudiginformatieobject
                .EnkelvoudigInformatieObjectVersies.Where(GetNotCompletedDocumentFilter(request.IgnoreNotCompletedDocuments))
                .OrderBy(e => e.Versie)
                .LastOrDefault();
        }
        else if (filter.Versie.HasValue && !filter.RegistratieOp.HasValue)
        {
            // Filter 'versie' is specified so get that version
            requestedVersie = enkelvoudiginformatieobject
                .EnkelvoudigInformatieObjectVersies.Where(GetNotCompletedDocumentFilter(request.IgnoreNotCompletedDocuments))
                .SingleOrDefault(e => e.Versie == filter.Versie);
        }
        else if (!filter.Versie.HasValue && filter.RegistratieOp.HasValue)
        {
            // Filter 'registratieOp' is specified so get the nearest versie before this date-time
            requestedVersie = enkelvoudiginformatieobject
                .EnkelvoudigInformatieObjectVersies.Where(GetNotCompletedDocumentFilter(request.IgnoreNotCompletedDocuments))
                .OrderBy(e => e.BeginRegistratie)
                .LastOrDefault(e => e.BeginRegistratie <= filter.RegistratieOp);
        }
        else
        {
            // Filter 'versie' and 'registratieOp' are specified
            requestedVersie = enkelvoudiginformatieobject
                .EnkelvoudigInformatieObjectVersies.Where(GetNotCompletedDocumentFilter(request.IgnoreNotCompletedDocuments))
                .Where(e => e.BeginRegistratie <= filter.RegistratieOp && e.Versie == filter.Versie)
                .SingleOrDefault();
        }

        if (requestedVersie == null)
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.NotFound);
        }

        if (!AuthorizationContextAccessor.AuthorizationContext.IsAuthorized(requestedVersie))
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.Forbidden);
        }

        // Get the requested version only
        enkelvoudiginformatieobject.EnkelvoudigInformatieObjectVersies.Clear();
        enkelvoudiginformatieobject.EnkelvoudigInformatieObjectVersies.Add(requestedVersie);

        if (request.IgnoreLock)
        {
            enkelvoudiginformatieobject.Lock = null;
        }

        return new QueryResult<EnkelvoudigInformatieObject>(enkelvoudiginformatieobject, QueryStatus.OK);
    }

    private static Func<EnkelvoudigInformatieObjectVersie, bool> GetNotCompletedDocumentFilter(bool ignoreNotCompletedDocuments)
    {
        return v => !ignoreNotCompletedDocuments || v.BestandsDelen.Count == 0;
    }
}

class GetEnkelvoudigInformatieObjectQuery : IRequest<QueryResult<EnkelvoudigInformatieObject>>
{
    public Guid Id { get; internal set; }
    public GetEnkelvoudigInformatieObjectFilter GetEnkelvoudigInformatieObjectFilter { get; internal set; } =
        new GetEnkelvoudigInformatieObjectFilter();
    public bool IgnoreLock { get; internal set; }
    public bool IgnoreNotCompletedDocuments { get; internal set; }
}
