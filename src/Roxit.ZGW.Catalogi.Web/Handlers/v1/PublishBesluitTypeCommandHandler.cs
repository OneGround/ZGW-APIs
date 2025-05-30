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
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class PublishBesluitTypeCommandHandler
    : CatalogiBaseHandler<PublishBesluitTypeCommandHandler>,
        IRequestHandler<PublishBesluitTypeCommand, CommandResult<BesluitType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public PublishBesluitTypeCommandHandler(
        ILogger<PublishBesluitTypeCommandHandler> logger,
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

    public async Task<CommandResult<BesluitType>> Handle(PublishBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting BesluitType publish process.");

        var rsinFilter = GetRsinFilterPredicate<BesluitType>(b => b.Catalogus.Owner == _rsin);

        var besluitType = await _context
            .BesluitTypen.AsSplitQuery()
            .Where(rsinFilter)
            .Include(b => b.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluitType == null)
        {
            return new CommandResult<BesluitType>(null, CommandStatus.NotFound);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<BesluitTypeResponseDto>(besluitType);

            besluitType.Concept = false;

            var errors = new List<ValidationError>();

            var catalogusRsinFilter = GetRsinFilterPredicate<BesluitType>(b => b.Catalogus.Owner == _rsin);
            var besluitTypen = await _context
                .BesluitTypen.Include(c => c.Catalogus)
                .Where(catalogusRsinFilter)
                .Where(z => z.Id != besluitType.Id && z.Omschrijving == besluitType.Omschrijving)
                .ToListAsync(cancellationToken);
            if (!_conceptBusinessRule.ValidateGeldigheid(besluitTypen.OfType<IConceptEntity>().ToList(), besluitType, errors))
            {
                var error = new ValidationError(
                    "besluittype",
                    ErrorCode.Invalid,
                    $"Kan Besluittype niet publiceren omdat de omschrijving '{besluitType.Omschrijving}' al gebruikt is binnen de geldigheidsperiode."
                );
                return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
            }

            audittrail.SetNew<BesluitTypeResponseDto>(besluitType);

            await audittrail.PatchedAsync(besluitType.Catalogus, besluitType, "Besluittype gepubliceerd", cancellationToken);

            await _cacheInvalidator.InvalidateAsync(besluitType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        return new CommandResult<BesluitType>(besluitType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

class PublishBesluitTypeCommand : IRequest<CommandResult<BesluitType>>
{
    public Guid Id { get; set; }
}
