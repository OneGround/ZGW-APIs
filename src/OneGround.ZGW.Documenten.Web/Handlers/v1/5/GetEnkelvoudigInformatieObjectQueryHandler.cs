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

        // First, fetch the parent object (without versions)
        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var enkelvoudiginformatieobject = await _context
            .EnkelvoudigInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enkelvoudiginformatieobject == null)
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.NotFound);
        }

        var filter = request.GetEnkelvoudigInformatieObjectFilter;
        EnkelvoudigInformatieObjectVersie requestedVersie = null;

        // Build the base query for versions (use IQueryable to avoid type issues)
        var rsinFilterVersion = GetRsinFilterPredicate<EnkelvoudigInformatieObjectVersie>();

        IQueryable<EnkelvoudigInformatieObjectVersie> versieQuery = _context
            .EnkelvoudigInformatieObjectVersies.AsNoTracking()
            .Where(rsinFilterVersion)
            .Where(v => v.EnkelvoudigInformatieObjectId == request.Id)
            .Include(v => v.BestandsDelen);

        // Apply filter logic using expressions
        var notCompletedFilter = GetNotCompletedDocumentFilterExpression(request.IgnoreNotCompletedDocuments);
        versieQuery = versieQuery.Where(notCompletedFilter);

        if (!filter.Versie.HasValue && !filter.RegistratieOp.HasValue)
        {
            // Latest version
            requestedVersie = await versieQuery.OrderBy(v => v.Versie).LastOrDefaultAsync(cancellationToken);
        }
        else if (filter.Versie.HasValue && !filter.RegistratieOp.HasValue)
        {
            // Specific version
            requestedVersie = await versieQuery.Where(v => v.Versie == filter.Versie).SingleOrDefaultAsync(cancellationToken);
        }
        else if (!filter.Versie.HasValue && filter.RegistratieOp.HasValue)
        {
            // Nearest version before date
            requestedVersie = await versieQuery
                .Where(v => v.BeginRegistratie <= filter.RegistratieOp)
                .OrderBy(v => v.BeginRegistratie)
                .LastOrDefaultAsync(cancellationToken);
        }
        else
        {
            // Both filters
            requestedVersie = await versieQuery
                .Where(v => v.BeginRegistratie <= filter.RegistratieOp && v.Versie == filter.Versie)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (requestedVersie == null)
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.NotFound);
        }

        enkelvoudiginformatieobject.LatestEnkelvoudigInformatieObjectVersie = requestedVersie;
        requestedVersie.InformatieObject = enkelvoudiginformatieobject;

        if (!AuthorizationContextAccessor.AuthorizationContext.IsAuthorized(requestedVersie))
        {
            return new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.Forbidden);
        }

        // Attach only the requested version
        enkelvoudiginformatieobject.EnkelvoudigInformatieObjectVersies.Clear();
        enkelvoudiginformatieobject.EnkelvoudigInformatieObjectVersies.Add(requestedVersie);

        if (request.IgnoreLock)
        {
            enkelvoudiginformatieobject.Lock = null;
        }

        return new QueryResult<EnkelvoudigInformatieObject>(enkelvoudiginformatieobject, QueryStatus.OK);
    }

    private static Expression<Func<EnkelvoudigInformatieObjectVersie, bool>> GetNotCompletedDocumentFilterExpression(bool ignoreNotCompletedDocuments)
    {
        if (!ignoreNotCompletedDocuments)
        {
            // No filter, always true
            return v => true;
        }
        else
        {
            // Only include versions with no BestandsDelen
            return v => v.BestandsDelen.Count == 0;
        }
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
