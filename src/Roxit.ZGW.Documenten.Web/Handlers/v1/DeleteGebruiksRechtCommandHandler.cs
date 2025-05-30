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
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Notificaties;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

class DeleteGebruiksRechtCommandHandler
    : DocumentenBaseHandler<DeleteGebruiksRechtCommandHandler>,
        IRequestHandler<DeleteGebruiksRechtCommand, CommandResult>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteGebruiksRechtCommandHandler(
        ILogger<DeleteGebruiksRechtCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteGebruiksRechtCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get GebruiksRecht {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<GebruiksRecht>(o => o.InformatieObject.Owner == _rsin);

        var gebruiksrecht = await _context
            .GebruiksRechten.Where(rsinFilter)
            .Include(g => g.InformatieObject)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (gebruiksrecht == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        int references = await _context.GebruiksRechten.CountAsync(
            g => g.Id != gebruiksrecht.Id && g.InformatieObjectId == gebruiksrecht.InformatieObjectId,
            cancellationToken: cancellationToken
        );
        if (references == 0)
        {
            gebruiksrecht.InformatieObject.IndicatieGebruiksrecht = null;
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting GebruiksRecht {Id}....", gebruiksrecht.Id);

            audittrail.SetOld<GebruiksRechtResponseDto>(gebruiksrecht);

            _context.GebruiksRechten.Remove(gebruiksrecht);

            await audittrail.DestroyedAsync(gebruiksrecht.InformatieObject, gebruiksrecht, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("GebruiksRecht {Id} successfully deleted.", gebruiksrecht.Id);
        }

        await SendNotificationAsync(Actie.destroy, gebruiksrecht, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" };
}

class DeleteGebruiksRechtCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
