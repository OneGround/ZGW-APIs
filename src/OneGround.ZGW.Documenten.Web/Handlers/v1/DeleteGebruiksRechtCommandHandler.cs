using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

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
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificationService, documentKenmerkenResolver)
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
            .Include(g => g.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (gebruiksrecht == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(gebruiksrecht.InformatieObject))
        {
            return new CommandResult(CommandStatus.Forbidden);
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
