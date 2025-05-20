using System;
using System.IO;
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
using OneGround.ZGW.Documenten.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class DownloadEnkelvoudigInformatieObjectQueryHandler
    : DocumentenBaseHandler<DownloadEnkelvoudigInformatieObjectQueryHandler>,
        IRequestHandler<DownloadEnkelvoudigInformatieObjectQuery, QueryResult<Stream>>
{
    private readonly DrcDbContext _context;
    private readonly IDocumentServicesResolver _documentServicesResolver;

    public DownloadEnkelvoudigInformatieObjectQueryHandler(
        ILogger<DownloadEnkelvoudigInformatieObjectQueryHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IDocumentServicesResolver documentServicesResolver,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _documentServicesResolver = documentServicesResolver;
    }

    public async Task<QueryResult<Stream>> Handle(DownloadEnkelvoudigInformatieObjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Download EnkelvoudigInformatieObject....");

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .SingleOrDefaultAsync(a => a.Id == request.EnkelvoudigInformatieObjectId, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            return new QueryResult<Stream>(null, QueryStatus.NotFound);
        }

        var service = _documentServicesResolver.Find(request.DocumentUrn.Type);
        if (service == null)
        {
            _logger.LogError("Could not find a document provider to handle {DocumentUrn}.", request.DocumentUrn);

            return new QueryResult<Stream>(null, QueryStatus.NotFound);
        }

        _logger.LogDebug("DMS provider to retrieve document from '{ProviderPrefix}' [{service}]", service.ProviderPrefix, service);

        var contents = await service.TryGetDocumentAsync(request.DocumentUrn, cancellationToken);
        if (contents == null)
        {
            _logger.LogError(
                "Document '{DocumentUrn}' [EnkelvoudigInformatieObjectId={EnkelvoudigInformatieObjectId}] does not exist. Is it moved?",
                request.DocumentUrn,
                request.EnkelvoudigInformatieObjectId
            );

            return new QueryResult<Stream>(null, QueryStatus.NotFound);
        }

        return new QueryResult<Stream>(contents, QueryStatus.OK);
    }
}

class DownloadEnkelvoudigInformatieObjectQuery : IRequest<QueryResult<Stream>>
{
    public DocumentUrn DocumentUrn { get; internal set; }
    public Guid EnkelvoudigInformatieObjectId { get; internal set; }
}
