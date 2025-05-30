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
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1._5.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.Notificaties;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1._5;

class DeleteVerzendingCommandHandler : DocumentenBaseHandler<DeleteVerzendingCommandHandler>, IRequestHandler<DeleteVerzendingCommand, CommandResult>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteVerzendingCommandHandler(
        ILogger<DeleteVerzendingCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificationService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificationService)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteVerzendingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Verzending {requestId}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Verzending>(o => o.InformatieObject.Owner == _rsin);

        var verzending = await _context
            .Verzendingen.Where(rsinFilter)
            .Include(g => g.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (verzending == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(verzending.InformatieObject))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting Verzending {verzendingId}....", verzending.Id);

            audittrail.SetOld<VerzendingResponseDto>(verzending);

            _context.Verzendingen.Remove(verzending);

            await audittrail.DestroyedAsync(verzending.InformatieObject, verzending, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Verzending {verzendingId} successfully deleted.", verzending.Id);
        }

        await SendNotificationAsync(Actie.destroy, verzending, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "Verzending" };
}

class DeleteVerzendingCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
