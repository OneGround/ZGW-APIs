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
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class DeleteEigenschapCommandHandler : CatalogiBaseHandler<DeleteEigenschapCommandHandler>, IRequestHandler<DeleteEigenschapCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteEigenschapCommandHandler(
        ILogger<DeleteEigenschapCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteEigenschapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Eigenschap {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Eigenschap>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var eigenschap = await _context
            .Eigenschappen.Include(z => z.ZaakType)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (eigenschap == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(eigenschap.ZaakType, errors))
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Deleting Eigenschap {Id}....", eigenschap.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<EigenschapResponseDto>(eigenschap);

            _context.Eigenschappen.Remove(eigenschap);

            await audittrail.DestroyedAsync(eigenschap.ZaakType, eigenschap, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Eigenschap {Id} successfully deleted.", eigenschap.Id);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "eigenschap" };
}

class DeleteEigenschapCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
