using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

class DeleteObjectInformatieObjectCommandHandler
    : DocumentenBaseHandler<DeleteObjectInformatieObjectCommandHandler>,
        IRequestHandler<DeleteObjectInformatieObjectCommand, CommandResult>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteObjectInformatieObjectCommandHandler(
        ILogger<DeleteObjectInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteObjectInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ObjectInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ObjectInformatieObject>(o => o.InformatieObject.Owner == _rsin);

        var objectInformatieObject = await _context
            .ObjectInformatieObjecten.Where(rsinFilter)
            .Include(e => e.InformatieObject.ObjectInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (objectInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ObjectInformatieObject {Id}....", objectInformatieObject.Id);

            audittrail.SetOld<ObjectInformatieObjectResponseDto>(objectInformatieObject);

            _context.ObjectInformatieObjecten.Remove(objectInformatieObject);

            await audittrail.DestroyedAsync(objectInformatieObject.InformatieObject, objectInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ObjectInformatieObject {Id} successfully deleted.", objectInformatieObject.Id);
        }

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "objectinformatieobject" };
}

class DeleteObjectInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
