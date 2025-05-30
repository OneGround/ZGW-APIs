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
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

public class UpdateEigenschapCommandHandler
    : CatalogiBaseHandler<UpdateEigenschapCommandHandler>,
        IRequestHandler<UpdateEigenschapCommand, CommandResult<Eigenschap>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IEntityUpdater<Eigenschap> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateEigenschapCommandHandler(
        ILogger<UpdateEigenschapCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        IConceptBusinessRule conceptBusinessRule,
        IEntityUpdater<Eigenschap> entityUpdater,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<Eigenschap>> Handle(UpdateEigenschapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Eigenschap {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Eigenschap>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var eigenschap = await _context
            .Eigenschappen.Where(rsinFilter)
            .Include(s => s.ZaakType.Catalogus)
            .Include(s => s.StatusType)
            .Include(s => s.Specificatie)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (eigenschap == null)
        {
            return new CommandResult<Eigenschap>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(eigenschap.ZaakType, errors))
        {
            return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var zaakTypeId = _uriService.GetId(request.ZaakType);
        var zaakType = await _context.ZaakTypen.SingleOrDefaultAsync(z => z.Id == zaakTypeId, cancellationToken);
        if (zaakType == null)
        {
            errors.Add(new ValidationError("zaaktype", ErrorCode.Invalid, $"ZaakType {request.ZaakType} is onbekend."));
            return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        StatusType statusType = null;
        if (request.StatusType != null)
        {
            var rsinFilterStatusType = GetRsinFilterPredicate<StatusType>(t => t.Owner == _rsin);
            var statusTypeId = _uriService.GetId(request.StatusType);
            statusType = await _context.StatusTypen.Where(rsinFilterStatusType).SingleOrDefaultAsync(z => z.Id == statusTypeId, cancellationToken);
            if (statusType == null)
            {
                var error = new ValidationError("statustype", ErrorCode.NotFound, $"Statustype '{request.StatusType}' niet gevonden.");
                return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, error);
            }
        }

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakType, errors))
        {
            return new CommandResult<Eigenschap>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating Eigenschap {Id}....", eigenschap.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<EigenschapResponseDto>(eigenschap);

            if (zaakType.Id != eigenschap.ZaakTypeId)
            {
                eigenschap.ZaakType = zaakType;
                eigenschap.ZaakTypeId = zaakType.Id;
                eigenschap.Owner = zaakType.Owner;
                if (eigenschap.Specificatie != null)
                {
                    eigenschap.Specificatie.Owner = zaakType.Owner;
                }
            }

            if (statusType == null)
            {
                eigenschap.StatusType = null;
                eigenschap.StatusTypeId = null;
            }
            else if (statusType.Id != eigenschap.StatusTypeId)
            {
                eigenschap.StatusType = statusType;
                eigenschap.StatusTypeId = statusType.Id;
            }

            _entityUpdater.Update(request.Eigenschap, eigenschap, version: 1.3M);

            audittrail.SetNew<EigenschapResponseDto>(eigenschap);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(eigenschap.ZaakType, eigenschap, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(eigenschap.ZaakType, eigenschap, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Eigenschap {Id} successfully updated.", eigenschap.Id);

        return new CommandResult<Eigenschap>(eigenschap, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "eigenschap" };
}

public class UpdateEigenschapCommand : IRequest<CommandResult<Eigenschap>>
{
    public Eigenschap Eigenschap { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakType { get; internal set; }
    public string StatusType { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
