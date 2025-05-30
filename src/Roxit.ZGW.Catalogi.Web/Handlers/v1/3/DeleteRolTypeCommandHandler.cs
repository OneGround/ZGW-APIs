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
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class DeleteRolTypeCommandHandler : CatalogiBaseHandler<DeleteRolTypeCommandHandler>, IRequestHandler<DeleteRolTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteRolTypeCommandHandler(
        ILogger<DeleteRolTypeCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteRolTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get RolType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<RolType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var rolType = await _context
            .RolTypen.Include(z => z.ZaakType)
            .ThenInclude(z => z.Catalogus)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (rolType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(rolType.ZaakType, errors))
        {
            return new CommandResult<RolType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Deleting RolType {Id}....", rolType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<RolTypeResponseDto>(rolType);

            _context.RolTypen.Remove(rolType);

            await _cacheInvalidator.InvalidateAsync(rolType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(rolType);

            await audittrail.DestroyedAsync(rolType.ZaakType, rolType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("RolType {Id} successfully deleted.", rolType.Id);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "roltype" };
}

class DeleteRolTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
