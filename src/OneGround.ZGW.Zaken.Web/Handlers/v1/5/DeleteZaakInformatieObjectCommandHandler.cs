using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class DeleteZaakInformatieObjectCommandHandler
    : ZakenBaseHandler<DeleteZaakInformatieObjectCommandHandler>,
        IRequestHandler<DeleteZaakInformatieObjectCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public DeleteZaakInformatieObjectCommandHandler(
        ILogger<DeleteZaakInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult> Handle(DeleteZaakInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakInformatieObject>();

        var zaakInformatieObject = await _context
            .ZaakInformatieObjecten.Where(rsinFilter)
            .Include(z => z.Zaak)
            .ThenInclude(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakInformatieObject.Zaak, errors))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        // keep the reference for later, because saving context removed relation from zaakInformatieObject.Zaak
        var zaak = zaakInformatieObject.Zaak;
        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakInformatieObject {Id}....", zaakInformatieObject.Id);

            audittrail.SetOld<ZaakInformatieObjectResponseDto>(zaakInformatieObject);

            _context.ZaakInformatieObjecten.Remove(zaakInformatieObject);

            await audittrail.DestroyedAsync(zaakInformatieObject.Zaak, zaakInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakInformatieObject {Id} successfully deleted.", zaakInformatieObject.Id);
        }

        var extraKenmerken = new Dictionary<string, string> { { "zaakinformatieobject.informatieobject", zaakInformatieObject.InformatieObject } };

        await SendNotificationAsync(Actie.destroy, zaakInformatieObject, extraKenmerken, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" };
}

class DeleteZaakInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
