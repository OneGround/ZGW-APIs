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
using Roxit.ZGW.Catalogi.Web.Authorization;
using Roxit.ZGW.Catalogi.Web.BusinessRules;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class UpdateZaakObjectTypeCommandHandler
    : CatalogiBaseHandler<UpdateZaakObjectTypeCommandHandler>,
        IRequestHandler<UpdateZaakObjectTypeCommand, CommandResult<ZaakObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IEntityUpdater<ZaakObjectType> _entityUpdater;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateZaakObjectTypeCommandHandler(
        ILogger<UpdateZaakObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IEntityUpdater<ZaakObjectType> entityUpdater,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakObjectType>> Handle(UpdateZaakObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakObjectType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var zaakObjectType = await _context
            .ZaakObjectTypen.Where(rsinFilter)
            .Include(s => s.ZaakType.Catalogus)
            // TODO: We ask VNG how the relations can be edited:
            //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
            //.Include(s => s.ResultaatTypen)
            //.Include(s => s.StatusTypen)
            // ----
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakObjectType == null)
        {
            return new CommandResult<ZaakObjectType>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakObjectType.ZaakType, errors))
        {
            return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var zaakTypeRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context.ZaakTypen.Where(zaakTypeRsinFilter).SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            errors.Add(new ValidationError("zaaktype", ErrorCode.Invalid, $"ZaakType {request.ZaakType} is onbekend."));
            return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        // Check rules on non-concept (published) zaaktype and scope ".geforceerd-bijwerken" (or hasAllAuthorizations)
        if (IsPublishedZaakTypeAndCanBeUpdated(zaakType))
        {
            ValidateZaakObjectTypeChanges(request, zaakObjectType, zaakType, errors);
        }

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!AuthorizationContextAccessor.AuthorizationContext.IsForcedUpdateAuthorized())
        {
            if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
            {
                return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501

        //var rsinFilterZotRt = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);
        //if (await _context.ResultaatTypen
        //    .Where(rsinFilterZotRt)
        //    .AnyAsync(a => a.ZaakObjectTypeId == zaakObjectType.Id && zaakType.Id != a.ZaakTypeId, cancellationToken))
        //{
        //    errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Het zaakobjecttype is in gebruik bij resultaattype en kan niet worden gewijzigd."));
        //}

        //var rsinFilterZotSt = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);
        //if (await _context.StatusTypen
        //    .Where(rsinFilterZotSt)
        //    .AnyAsync(a => a.ZaakObjectTypeId == zaakObjectType.Id && zaakType.Id != a.ZaakTypeId, cancellationToken))
        //{
        //    errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Het zaakobjecttype is in gebruik bij statustype en kan niet worden gewijzigd."));
        //}

        //if (errors.Any())
        //{
        //    return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        //}
        // ----

        _logger.LogDebug("Updating ZaakObjectType {Id}....", zaakObjectType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakObjectTypeResponseDto>(zaakObjectType);

            if (zaakType.Id != zaakObjectType.ZaakTypeId)
            {
                zaakObjectType.ZaakType = zaakType;
                zaakObjectType.ZaakTypeId = zaakType.Id;
                zaakObjectType.Owner = zaakObjectType.ZaakType.Owner;
            }

            _entityUpdater.Update(request.ZaakObjectType, zaakObjectType, version: 1.3M);

            audittrail.SetNew<ZaakObjectTypeResponseDto>(zaakObjectType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakObjectType.ZaakType, zaakObjectType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakObjectType.ZaakType, zaakObjectType, cancellationToken);
            }

            await _cacheInvalidator.InvalidateAsync(zaakObjectType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(zaakObjectType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakObjectType {Id} successfully updated.", zaakObjectType.Id);

        return new CommandResult<ZaakObjectType>(zaakObjectType, CommandStatus.OK);
    }

    private bool IsPublishedZaakTypeAndCanBeUpdated(ZaakType zaaktype)
    {
        return !zaaktype.Concept && AuthorizationContextAccessor.AuthorizationContext.IsForcedUpdateAuthorized();
    }

    private void ValidateZaakObjectTypeChanges(
        UpdateZaakObjectTypeCommand request,
        ZaakObjectType zaakObjectType,
        ZaakType zaakType,
        List<ValidationError> errors
    )
    {
        // Be sure zaaktype is not changed
        if (_uriService.GetId(request.ZaakType) != zaakType.Id)
        {
            var error = new ValidationError(
                "zaaktype",
                ErrorCode.Invalid,
                $"Het is niet toegestaan van een gepubliceerd zaaktype met scope '{AuthorizationScopes.Catalogi.ForcedUpdate}' de zaaktype van het zaakobjecttype aan te passen."
            );
            errors.Add(error);
        }

        // Be sure normal fields are not changed
        if (!zaakObjectType.CanBeUpdated(request.ZaakObjectType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Invalid,
                $"Het is niet toegestaan van een gepubliceerd zaaktype met scope '{AuthorizationScopes.Catalogi.ForcedUpdate}' één of meerdere veld(en) van het zaakobjecttype te wijzigen."
            );
            errors.Add(error);
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaakobjecttype" };
}

class UpdateZaakObjectTypeCommand : IRequest<CommandResult<ZaakObjectType>>
{
    public ZaakObjectType ZaakObjectType { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; internal set; }
    public IEnumerable<string> Eigenschappen { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
