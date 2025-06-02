using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class UpdateZaakTypeInformatieObjectTypeHandler
    : CatalogiBaseHandler<UpdateZaakTypeInformatieObjectTypeHandler>,
        IRequestHandler<UpdateZaakTypeInformatieObjectTypeCommand, CommandResult<ZaakTypeInformatieObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IZaakTypeInformatieObjectTypenBusinessRuleService _businessRuleService;
    private readonly IEntityUpdater<ZaakTypeInformatieObjectType> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ICacheInvalidator _cacheInvalidator;

    public UpdateZaakTypeInformatieObjectTypeHandler(
        ILogger<UpdateZaakTypeInformatieObjectTypeHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IZaakTypeInformatieObjectTypenBusinessRuleService businessRuleService,
        IEntityUpdater<ZaakTypeInformatieObjectType> entityUpdater,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory,
        ICacheInvalidator cacheInvalidator
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _businessRuleService = businessRuleService;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<CommandResult<ZaakTypeInformatieObjectType>> Handle(
        UpdateZaakTypeInformatieObjectTypeCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get ZaakTypeInformatieObjectType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var zaakTypeInformatieObjectType = await _context
            .ZaakTypeInformatieObjectTypen.Where(rsinFilter)
            .Include(z => z.ZaakType)
            .Include(z => z.StatusType)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakTypeInformatieObjectType == null)
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (
            !await _businessRuleService.ValidateExistsAsync(
                zaakTypeInformatieObjectType.Id,
                zaakTypeInformatieObjectType.ZaakType.Id,
                request.ZaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving,
                request.ZaakTypeInformatieObjectType.VolgNummer,
                request.ZaakTypeInformatieObjectType.Richting,
                errors
            )
        )
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        // Note: On a published zaaktype related informatieobjecttype (zaaktype-informatieobjecttype) changes are not allowed to make
        if (!_conceptBusinessRule.ValidateConceptRelation(zaakTypeInformatieObjectType.ZaakType, errors, version: 1.3M))
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var zaakTypeFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);

        var zaakType = await _context
            .ZaakTypen.Where(zaakTypeFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakType), cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.NotFound, error);
        }

        // Note: On a published zaaktype changes are not allowed to make
        if (
            _uriService.GetUri(zaakType.Url) != _uriService.GetUri(zaakTypeInformatieObjectType.ZaakType.Url)
            && !_conceptBusinessRule.ValidateConceptRelation(zaakType, errors, version: 1.3M)
        )
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating ZaakTypeInformatieObjectType {Id}....", zaakTypeInformatieObjectType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeInformatieObjectTypeResponseDto>(zaakTypeInformatieObjectType);

            var statusTypeFilter = GetRsinFilterPredicate<StatusType>(t => t.ZaakType.Catalogus.Owner == _rsin);

            var statusType = await _context
                .StatusTypen.Where(statusTypeFilter)
                .SingleOrDefaultAsync(s => s.Id == _uriService.GetId(request.StatusType), cancellationToken);

            if (zaakType.Id != zaakTypeInformatieObjectType.ZaakTypeId)
            {
                _logger.LogDebug("Updating zaakType {ZaakType}....", request.ZaakType);

                zaakTypeInformatieObjectType.ZaakType = zaakType;
                zaakTypeInformatieObjectType.ZaakTypeId = zaakType.Id;
                zaakTypeInformatieObjectType.Owner = zaakTypeInformatieObjectType.ZaakType.Owner;
            }

            _logger.LogDebug("Updating informatieObjectType {InformatieObjectType}....", request.InformatieObjectType);

            // Note: In v1.3 there is a soft relation based on omschrijving instead of iot-url
            zaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving = request.ZaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving;

            if (statusType?.Id != zaakTypeInformatieObjectType?.StatusTypeId) // Note: Both of these fields can be null
            {
                _logger.LogDebug("Updating statusType {StatusType}....", request.StatusType);

                zaakTypeInformatieObjectType.StatusType = statusType;
                zaakTypeInformatieObjectType.StatusTypeId = statusType?.Id;
            }

            _entityUpdater.Update(request.ZaakTypeInformatieObjectType, zaakTypeInformatieObjectType);

            audittrail.SetNew<ZaakTypeInformatieObjectTypeResponseDto>(zaakTypeInformatieObjectType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakTypeInformatieObjectType.ZaakType, zaakTypeInformatieObjectType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakTypeInformatieObjectType.ZaakType, zaakTypeInformatieObjectType, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _cacheInvalidator.InvalidateAsync(zaakTypeInformatieObjectType.ZaakType);

        _logger.LogDebug("ZaakTypeInformatieObjectType {Id} successfully updated.", zaakTypeInformatieObjectType.Id);

        return new CommandResult<ZaakTypeInformatieObjectType>(zaakTypeInformatieObjectType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype-informatieobjecttypen" };
}

class UpdateZaakTypeInformatieObjectTypeCommand : IRequest<CommandResult<ZaakTypeInformatieObjectType>>
{
    public ZaakTypeInformatieObjectType ZaakTypeInformatieObjectType { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; set; }
    public string StatusType { get; set; }
    public string InformatieObjectType { get; set; }
    public bool IsPartialUpdate { get; internal set; }
}
