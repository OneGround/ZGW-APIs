using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Notificaties;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class DeleteInformatieObjectTypeCommandHandler
    : CatalogiBaseHandler<DeleteInformatieObjectTypeCommandHandler>,
        IRequestHandler<DeleteInformatieObjectTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public DeleteInformatieObjectTypeCommandHandler(
        ILogger<DeleteInformatieObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IInformatieObjectTypeDataService informatieObjectTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _informatieObjectTypeDataService = informatieObjectTypeDataService;
    }

    public async Task<CommandResult> Handle(DeleteInformatieObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get InformatieObjectType {Id}....", request.Id);

        var informatieObjectType = await _informatieObjectTypeDataService.GetAsync(request.Id, cancellationToken, trackingChanges: true);
        if (informatieObjectType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!informatieObjectType.Concept)
        {
            return new CommandResult<ZaakType>(
                null,
                CommandStatus.ValidationError,
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.NonConceptObject,
                    "Het is niet toegestaan om een non-concept informatieobjecttype te verwijderen."
                )
            );
        }

        _logger.LogDebug("Deleting InformatieObjectType {Id}....", informatieObjectType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<InformatieObjectTypeResponseDto>(informatieObjectType);

            _context.InformatieObjectTypen.Remove(informatieObjectType);

            await audittrail.DestroyedAsync(informatieObjectType.Catalogus, informatieObjectType, cancellationToken);

            await _cacheInvalidator.InvalidateAsync(
                informatieObjectType.InformatieObjectTypeZaakTypen.Select(t => t.ZaakType),
                informatieObjectType.Catalogus.Owner
            );
            await _cacheInvalidator.InvalidateAsync(
                informatieObjectType.InformatieObjectTypeBesluitTypen.Select(t => t.InformatieObjectType),
                informatieObjectType.Catalogus.Owner
            );
            await _cacheInvalidator.InvalidateAsync(informatieObjectType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("InformatieObjectType {Id} successfully deleted.", informatieObjectType.Id);

        await SendNotificationAsync(Actie.destroy, informatieObjectType, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "informatieobjecttype" };
}

class DeleteInformatieObjectTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
