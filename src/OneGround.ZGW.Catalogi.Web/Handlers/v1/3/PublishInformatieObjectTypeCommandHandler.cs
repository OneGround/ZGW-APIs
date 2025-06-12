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
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class PublishInformatieObjectTypeCommandHandler
    : CatalogiBaseHandler<PublishInformatieObjectTypeCommandHandler>,
        IRequestHandler<PublishInformatieObjectTypeCommand, CommandResult<InformatieObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IInformatieObjectTypeDataService _informatieObjectTypeDataService;

    public PublishInformatieObjectTypeCommandHandler(
        ILogger<PublishInformatieObjectTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IInformatieObjectTypeDataService informatieObjectTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _informatieObjectTypeDataService = informatieObjectTypeDataService;
    }

    public async Task<CommandResult<InformatieObjectType>> Handle(PublishInformatieObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting InformatieObjectType publish process.");

        var rsinFilter = GetRsinFilterPredicate<InformatieObjectType>(b => b.Catalogus.Owner == _rsin);

        var informatieObjectType = await _context
            .InformatieObjectTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (informatieObjectType == null)
        {
            return new CommandResult<InformatieObjectType>(null, CommandStatus.NotFound);
        }

        if (!informatieObjectType.Concept)
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.NonConceptObject, "Het Informatieobjecttype is al gepubliceerd.");
            return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<InformatieObjectTypeResponseDto>(informatieObjectType);

            // Check geldigheid while publishing current IOT (no overlaps in period of all PUBLISHED IOT'en)
            informatieObjectType.Concept = false;

            var errors = new List<ValidationError>();

            var informatieObjectTypen = await _context
                .InformatieObjectTypen.Include(c => c.Catalogus)
                .Where(rsinFilter)
                .Where(z => z.CatalogusId == informatieObjectType.CatalogusId)
                .Where(z => z.Id != informatieObjectType.Id && z.Omschrijving == informatieObjectType.Omschrijving)
                .ToListAsync(cancellationToken);
            if (!_conceptBusinessRule.ValidateGeldigheid(informatieObjectTypen.OfType<IConceptEntity>().ToList(), informatieObjectType, errors))
            {
                var error = new ValidationError(
                    "informatieobjecttype",
                    ErrorCode.Invalid,
                    $"Kan InformatieObjectType niet publiceren omdat de omschrijving '{informatieObjectType.Omschrijving}' al gebruikt is binnen de geldigheidsperiode."
                );
                return new CommandResult<InformatieObjectType>(null, CommandStatus.ValidationError, error);
            }

            audittrail.SetNew<InformatieObjectTypeResponseDto>(informatieObjectType);

            await audittrail.PatchedAsync(
                informatieObjectType.Catalogus,
                informatieObjectType,
                "Informatieobjecttype gepubliceerd",
                cancellationToken
            );

            await _cacheInvalidator.InvalidateAsync(informatieObjectType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("InformatieObjectType {Id} successfully published.", informatieObjectType.Id);

        // Note: Refresh updated InformatieObjectType with all sub-entities within geldigheid which was not loaded
        informatieObjectType = await _informatieObjectTypeDataService.GetAsync(request.Id, cancellationToken);

        return new CommandResult<InformatieObjectType>(informatieObjectType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "informatieobjecttype" };
}

class PublishInformatieObjectTypeCommand : IRequest<CommandResult<InformatieObjectType>>
{
    public Guid Id { get; set; }
}
