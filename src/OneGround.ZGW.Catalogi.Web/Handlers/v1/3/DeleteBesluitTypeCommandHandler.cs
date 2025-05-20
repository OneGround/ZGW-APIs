using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Notificaties;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class DeleteBesluitTypeCommandHandler : CatalogiBaseHandler<DeleteBesluitTypeCommandHandler>, IRequestHandler<DeleteBesluitTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public DeleteBesluitTypeCommandHandler(
        ILogger<DeleteBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<CommandResult> Handle(DeleteBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitType {Id}....", request.Id);

        var besluitType = await _besluitTypeDataService.GetAsync(request.Id, trackingChanges: true, cancellationToken: cancellationToken);
        if (besluitType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!besluitType.Concept)
        {
            return new CommandResult<BesluitType>(
                null,
                CommandStatus.ValidationError,
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.NonConceptObject,
                    "Het is niet toegestaan om een non-concept besluittype te verwijderen."
                )
            );
        }

        _logger.LogDebug("Deleting BesluitType {Id}....", besluitType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<BesluitTypeResponseDto>(besluitType);

            _context.BesluitTypen.Remove(besluitType);

            await audittrail.DestroyedAsync(besluitType.Catalogus, besluitType, cancellationToken);

            await _cacheInvalidator.InvalidateAsync(besluitType);
            await _cacheInvalidator.InvalidateAsync(
                besluitType.BesluitTypeInformatieObjectTypen.Select(t => t.InformatieObjectType),
                besluitType.Catalogus.Owner
            );

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("BesluitType {Id} successfully deleted.", besluitType.Id);

        await SendNotificationAsync(Actie.destroy, besluitType, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

class DeleteBesluitTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
