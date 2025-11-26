using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class DeleteBesluitInformatieObjectCommandHandler
    : BesluitenBaseHandler<DeleteBesluitInformatieObjectCommandHandler>,
        IRequestHandler<DeleteBesluitInformatieObjectCommand, CommandResult>
{
    private readonly BrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteBesluitInformatieObjectCommandHandler(
        ILogger<DeleteBesluitInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, besluitKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteBesluitInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<BesluitInformatieObject>(o => o.Besluit.Owner == _rsin);

        var besluitInformatieObject = await _context
            .BesluitInformatieObjecten.Where(rsinFilter)
            .Include(z => z.Besluit.BesluitInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluitInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        // keep the reference for later, because saving context removed relation from besluitInformatieObject.Besluit
        var besluit = besluitInformatieObject.Besluit;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting BesluitInformatieObject {Id}....", besluitInformatieObject.Id);

            audittrail.SetOld<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

            _context.BesluitInformatieObjecten.Remove(besluitInformatieObject);

            await audittrail.DestroyedAsync(besluitInformatieObject.Besluit, besluitInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("BesluitInformatieObject {Id} successfully deleted.", besluitInformatieObject.Id);
        }

        var extraKenmerken = new Dictionary<string, string>
        {
            { "besluitinformatieobject.informatieobject", besluitInformatieObject.InformatieObject },
        };

        await SendNotificationAsync(Actie.destroy, besluitInformatieObject, extraKenmerken, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluitinformatieobject" };
}

class DeleteBesluitInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
