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

class DeleteZaakObjectTypeCommandHandler
    : CatalogiBaseHandler<DeleteZaakObjectTypeCommandHandler>,
        IRequestHandler<DeleteZaakObjectTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteZaakObjectTypeCommandHandler(
        ILogger<DeleteZaakObjectTypeCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteZaakObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakObjectType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var zaakObjectType = await _context
            .ZaakObjectTypen.Include(z => z.ZaakType)
            .ThenInclude(z => z.Catalogus)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakObjectType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_conceptBusinessRule.ValidateConceptZaakType(zaakObjectType.ZaakType, errors))
        {
            return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501

        //var rsinFilterZotRt = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);
        //if (await _context.ResultaatTypen
        //    .Where(rsinFilterZotRt)
        //    .AnyAsync(a => a.ZaakObjectTypeId == zaakObjectType.Id, cancellationToken))
        //{
        //    errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Het zaakobjecttype is in gebruik bij resultaattype en kan niet worden verwijderd."));
        //}

        //var rsinFilterZotSt = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);
        //if (await _context.StatusTypen
        //    .Where(rsinFilterZotSt)
        //    .AnyAsync(a => a.ZaakObjectTypeId == zaakObjectType.Id, cancellationToken))
        //{
        //    errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Het zaakobjecttype is in gebruik bij statustype en kan niet worden verwijderd."));
        //}

        //if (errors.Any())
        //{
        //    return new CommandResult<ZaakObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        //}
        // ----

        _logger.LogDebug("Deleting ZaakObjectType {Id}....", zaakObjectType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakObjectTypeResponseDto>(zaakObjectType);

            _context.ZaakObjectTypen.Remove(zaakObjectType);

            await _cacheInvalidator.InvalidateAsync(zaakObjectType.ZaakType);
            await _cacheInvalidator.InvalidateAsync(zaakObjectType);

            await audittrail.DestroyedAsync(zaakObjectType.ZaakType, zaakObjectType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakObjectType {Id} successfully deleted.", zaakObjectType.Id);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaakobjecttype" };
}

class DeleteZaakObjectTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
