using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class PublishBesluitTypeCommandHandler
    : CatalogiBaseHandler<PublishBesluitTypeCommandHandler>,
        IRequestHandler<PublishBesluitTypeCommand, CommandResult<BesluitType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public PublishBesluitTypeCommandHandler(
        ILogger<PublishBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<CommandResult<BesluitType>> Handle(PublishBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting BesluitType publish process.");

        var rsinFilter = GetRsinFilterPredicate<BesluitType>(b => b.Catalogus.Owner == _rsin);

        var besluitType = await _context
            .BesluitTypen.Where(rsinFilter)
            .Include(b => b.Catalogus)
            .Include(z => z.BesluitTypeInformatieObjectTypen)
            .AsSplitQuery()
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluitType == null)
        {
            return new CommandResult<BesluitType>(null, CommandStatus.NotFound);
        }

        if (!besluitType.Concept)
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.NonConceptObject, "Het Besluittype is al gepubliceerd.");
            return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<BesluitTypeResponseDto>(besluitType);

            // Check geldigheid while publishing current BT (no overlaps in period of all PUBLISHED BT'en)
            besluitType.Concept = false;

            var errors = new List<ValidationError>();

            var besluitTypen = await _context
                .BesluitTypen.Include(c => c.Catalogus)
                .Where(rsinFilter)
                .Where(z => z.CatalogusId == besluitType.CatalogusId)
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

        _logger.LogDebug("BesluitType {Id} successfully published.", besluitType.Id);

        // Note: Refresh updated BesluitType with all sub-entities within geldigheid which was not loaded
        besluitType = await _besluitTypeDataService.GetAsync(request.Id, cancellationToken: cancellationToken);

        return new CommandResult<BesluitType>(besluitType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

class PublishBesluitTypeCommand : IRequest<CommandResult<BesluitType>>
{
    public Guid Id { get; set; }
}
