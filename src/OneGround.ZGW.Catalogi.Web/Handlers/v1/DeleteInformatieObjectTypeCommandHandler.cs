using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
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

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class DeleteInformatieObjectTypeCommandHandler
    : CatalogiBaseHandler<DeleteInformatieObjectTypeCommandHandler>,
        IRequestHandler<DeleteInformatieObjectTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly AuthorizationContext _authorizationContext;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public DeleteInformatieObjectTypeCommandHandler(
        ILogger<DeleteInformatieObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IInformatieObjectTypeDataService informatieObjectTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
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

        var errors = new List<ValidationError>();

        if (!_authorizationContext.IsForcedDeleteAuthorized())
        {
            if (!_conceptBusinessRule.ValidateConcept(informatieObjectType, errors))
            {
                return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            foreach (var besluitType in informatieObjectType.InformatieObjectTypeBesluitTypen.Select(t => t.BesluitType))
            {
                if (!_conceptBusinessRule.ValidateConceptRelation(besluitType, errors, version: 1.0M))
                {
                    return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
                }
            }
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
