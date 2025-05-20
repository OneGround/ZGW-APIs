using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

public class UpdateRolTypeCommandHandler
    : CatalogiBaseHandler<UpdateRolTypeCommandHandler>,
        IRequestHandler<UpdateRolTypeCommand, CommandResult<RolType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IEntityUpdater<RolType> _entityUpdater;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateRolTypeCommandHandler(
        ILogger<UpdateRolTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IEntityUpdater<RolType> entityUpdater,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _entityUpdater = entityUpdater;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<RolType>> Handle(UpdateRolTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get RolType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<RolType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var rolType = await _context
            .RolTypen.Where(rsinFilter)
            .Include(s => s.ZaakType)
            .ThenInclude(s => s.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (rolType == null)
        {
            return new CommandResult<RolType>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!AuthorizationContextAccessor.AuthorizationContext.IsForcedUpdateAuthorized())
        {
            if (!_conceptBusinessRule.ValidateConceptZaakType(rolType.ZaakType, errors))
            {
                return new CommandResult<RolType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        var zaakTypeRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context.ZaakTypen.Where(zaakTypeRsinFilter).SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);

        if (zaakType == null)
        {
            errors.Add(new ValidationError("zaaktype", ErrorCode.Invalid, $"ZaakType {request.ZaakType} is onbekend."));
            return new CommandResult<RolType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
        {
            return new CommandResult<RolType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating RolType {Id}....", rolType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<RolTypeResponseDto>(rolType);

            if (zaakType.Id != rolType.ZaakTypeId)
            {
                rolType.ZaakType = zaakType;
                rolType.ZaakTypeId = zaakType.Id;
                rolType.Owner = rolType.ZaakType.Owner;
            }

            _entityUpdater.Update(request.RolType, rolType);

            audittrail.SetNew<RolTypeResponseDto>(rolType);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(rolType.ZaakType, rolType, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(rolType.ZaakType, rolType, cancellationToken);
            }

            await _cacheInvalidator.InvalidateAsync(rolType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(rolType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("RolType {Id} successfully updated.", rolType.Id);

        return new CommandResult<RolType>(rolType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "roltype" };
}

public class UpdateRolTypeCommand : IRequest<CommandResult<RolType>>
{
    public RolType RolType { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
