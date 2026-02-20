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

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

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

        var filter = request.GetEnkelvoudigInformatieObjectFilter;
        var rsinFilterVersion = GetRsinFilterPredicate<EnkelvoudigInformatieObjectVersie>();

        // Build a single query that fetches both the parent object and the correct version
        IQueryable<EnkelvoudigInformatieObjectVersie> versieQuery = _context
            .EnkelvoudigInformatieObjectVersies.AsNoTracking()
            .Where(rsinFilterVersion)
            .Where(v => v.EnkelvoudigInformatieObjectId == request.Id)
            .Include(v => v.LatestInformatieObject)
            .Include(v => v.InformatieObject); // Include parent object in one query

        // Apply version filtering logic
        EnkelvoudigInformatieObjectVersie requestedVersie;

        if (!filter.Versie.HasValue && !filter.RegistratieOp.HasValue)
        {
            // Latest version (Filter: none)
            requestedVersie = await versieQuery.OrderBy(v => v.Versie).LastOrDefaultAsync(cancellationToken);
        }
        else if (filter.Versie.HasValue && !filter.RegistratieOp.HasValue)
        {
            // Specific version (Filter: 'versie')
            requestedVersie = await versieQuery.Where(v => v.Versie == filter.Versie).SingleOrDefaultAsync(cancellationToken);
        }
        else if (!filter.Versie.HasValue && filter.RegistratieOp.HasValue)
        {
            // Nearest version before date (Filter: 'registratieOp')
            requestedVersie = await versieQuery
                .Where(v => v.BeginRegistratie <= filter.RegistratieOp)
                .OrderBy(v => v.BeginRegistratie)
                .LastOrDefaultAsync(cancellationToken);
        }
        else
        {
            // Both filters (Filter: 'registratieOp' and 'versie')
            requestedVersie = await versieQuery
                .Where(v => v.BeginRegistratie <= filter.RegistratieOp && v.Versie == filter.Versie)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (requestedVersie == null)
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.NotFound);
        }

        var enkelvoudiginformatieobject = requestedVersie.InformatieObject;

        // Authorization check on the requested version
        if (!AuthorizationContextAccessor.AuthorizationContext.IsAuthorized(requestedVersie))
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.Forbidden);
        }

        // Set up the relationships
        enkelvoudiginformatieobject.LatestEnkelvoudigInformatieObjectVersie = requestedVersie;

        // Attach only the requested version
        enkelvoudiginformatieobject.EnkelvoudigInformatieObjectVersies.Clear();
        enkelvoudiginformatieobject.EnkelvoudigInformatieObjectVersies.Add(requestedVersie);

        if (request.IgnoreLock)
        {
            enkelvoudiginformatieobject.Lock = null;
        }

        return new QueryResult<EnkelvoudigInformatieObject>(enkelvoudiginformatieobject, QueryStatus.OK);
    }
}

class GetEnkelvoudigInformatieObjectQuery : IRequest<QueryResult<EnkelvoudigInformatieObject>>
{
    public Guid Id { get; internal set; }
    public GetEnkelvoudigInformatieObjectFilter GetEnkelvoudigInformatieObjectFilter { get; internal set; } =
        new GetEnkelvoudigInformatieObjectFilter();
    public bool IgnoreLock { get; internal set; }
}
