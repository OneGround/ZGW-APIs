using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Authorization;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
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

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class DeleteBesluitTypeCommandHandler : CatalogiBaseHandler<DeleteBesluitTypeCommandHandler>, IRequestHandler<DeleteBesluitTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly AuthorizationContext _authorizationContext;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public DeleteBesluitTypeCommandHandler(
        ILogger<DeleteBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _authorizationContext = authorizationContextAccessor.AuthorizationContext;
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

        var errors = new List<ValidationError>();

        if (!_authorizationContext.IsForcedDeleteAuthorized())
        {
            if (!_conceptBusinessRule.ValidateConcept(besluitType, errors))
            {
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
            }

            foreach (var informatieObjectType in besluitType.BesluitTypeInformatieObjectTypen.Select(t => t.InformatieObjectType))
            {
                if (!_conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M))
                {
                    return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, errors.ToArray());
                }
            }
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
