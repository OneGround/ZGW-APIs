using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
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

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class DeleteResultaatTypeCommandHandler
    : CatalogiBaseHandler<DeleteResultaatTypeCommandHandler>,
        IRequestHandler<DeleteResultaatTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteResultaatTypeCommandHandler(
        ILogger<DeleteResultaatTypeCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteResultaatTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ResultType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var resultType = await _context
            .ResultaatTypen.Include(z => z.ZaakType)
            .ThenInclude(z => z.Catalogus)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (resultType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(resultType.ZaakType, errors))
        {
            return new CommandResult<ResultaatType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Deleting ResultType {Id}....", resultType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ResultaatTypeResponseDto>(resultType);

            _context.ResultaatTypen.Remove(resultType);

            await _cacheInvalidator.InvalidateAsync(resultType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(resultType);

            await audittrail.DestroyedAsync(resultType.ZaakType, resultType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ResultType {Id} successfully deleted.", resultType.Id);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "resultaattype" };
}

class DeleteResultaatTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
