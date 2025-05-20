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

class DeleteZaakTypeCommandHandler : CatalogiBaseHandler<DeleteZaakTypeCommandHandler>, IRequestHandler<DeleteZaakTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly AuthorizationContext _authorizationContext;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public DeleteZaakTypeCommandHandler(
        ILogger<DeleteZaakTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
        _auditTrailFactory = auditTrailFactory;
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<CommandResult> Handle(DeleteZaakTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakType {Id}....", request.Id);

        var zaakType = await _zaakTypeDataService.GetAsync(request.Id, trackingChanges: true, cancellationToken: cancellationToken);
        if (zaakType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_authorizationContext.IsForcedDeleteAuthorized())
        {
            if (!_conceptBusinessRule.ValidateConcept(zaakType, errors))
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            foreach (var besluitType in zaakType.ZaakTypeBesluitTypen.Select(t => t.BesluitType))
            {
                if (!_conceptBusinessRule.ValidateConceptRelation(besluitType, errors, version: 1.0M))
                {
                    return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
                }
            }

            foreach (var informatieObjectType in zaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType))
            {
                if (!_conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M))
                {
                    return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
                }
            }
        }

        _logger.LogDebug("Deleting ZaakType {Id}....", zaakType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeResponseDto>(zaakType);

            _context.ZaakTypen.Remove(zaakType);

            await _cacheInvalidator.InvalidateAsync(zaakType.StatusTypen, zaakType.Catalogus.Owner);
            await _cacheInvalidator.InvalidateAsync(zaakType.ResultaatTypen, zaakType.Catalogus.Owner);
            await _cacheInvalidator.InvalidateAsync(
                zaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType),
                zaakType.Catalogus.Owner
            );
            await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeBesluitTypen.Select(t => t.BesluitType), zaakType.Catalogus.Owner);
            await _cacheInvalidator.InvalidateAsync(zaakType);

            await audittrail.DestroyedAsync(zaakType.Catalogus, zaakType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully deleted.", zaakType.Id);

        await SendNotificationAsync(Actie.destroy, zaakType, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" };
}

class DeleteZaakTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
