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
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

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
            .Include(s => s.ZaakType)
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

            _entityUpdater.Update(request.Eigenschap, eigenschap);

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
    public bool IsPartialUpdate { get; internal set; }
}
