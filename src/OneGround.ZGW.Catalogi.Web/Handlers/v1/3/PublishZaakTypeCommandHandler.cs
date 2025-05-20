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

class PublishZaakTypeCommandHandler
    : CatalogiBaseHandler<PublishZaakTypeCommandHandler>,
        IRequestHandler<PublishZaakTypeCommand, CommandResult<ZaakType>>
{
    private readonly ZtcDbContext _context;
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public PublishZaakTypeCommandHandler(
        ILogger<PublishZaakTypeCommandHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IConceptBusinessRule conceptBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _conceptBusinessRule = conceptBusinessRule;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<CommandResult<ZaakType>> Handle(PublishZaakTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting ZaakType publish process.");

        var rsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var zaakType = await _context
            .ZaakTypen.Where(rsinFilter)
            .Include(z => z.Catalogus)
            .Include(z => z.StatusTypen)
            .Include(z => z.RolTypen)
            .Include(z => z.ResultaatTypen)
            .Include(z => z.Eigenschappen)
            .Include(z => z.ReferentieProces)
            .Include(z => z.ZaakTypeInformatieObjectTypen)
            .Include(z => z.ZaakTypeDeelZaakTypen)
            .Include(z => z.ZaakTypeGerelateerdeZaakTypen)
            .Include(z => z.ZaakTypeBesluitTypen)
            .AsSplitQuery()
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakType == null)
        {
            return new CommandResult<ZaakType>(null, CommandStatus.NotFound);
        }

        if (!zaakType.Concept)
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.NonConceptObject, "Het Zaaktype is al gepubliceerd.");
            return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeResponseDto>(zaakType);

            // Check geldigheid while publishing current ZT (no overlaps in period of all PUBLISHED ZT'en)
            zaakType.Concept = false;

            var errors = new List<ValidationError>();

            var catalogusRsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);
            var zaakTypen = await _context
                .ZaakTypen.Include(c => c.Catalogus)
                .Where(catalogusRsinFilter)
                .Where(z => z.Id != zaakType.Id && z.Identificatie == zaakType.Identificatie)
                .ToListAsync(cancellationToken);
            if (!_conceptBusinessRule.ValidateGeldigheid(zaakTypen.OfType<IConceptEntity>().ToList(), zaakType, errors))
            {
                var error = new ValidationError(
                    "zaaktype",
                    ErrorCode.Invalid,
                    $"Kan Zaaktype niet publiceren omdat de identificatie '{zaakType.Identificatie}' al gebruikt is binnen de geldigheidsperiode."
                );
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, error);
            }

            audittrail.SetNew<ZaakTypeResponseDto>(zaakType);

            await audittrail.PatchedAsync(zaakType.Catalogus, zaakType, "Zaaktype gepubliceerd", cancellationToken);

            await _cacheInvalidator.InvalidateAsync(zaakType);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully published.", zaakType.Id);

        // Note: Refresh updated ZaakType with all sub-entities within geldigheid which was not loaded
        zaakType = await _zaakTypeDataService.GetAsync(request.Id, cancellationToken: cancellationToken);

        return new CommandResult<ZaakType>(zaakType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" };
}

class PublishZaakTypeCommand : IRequest<CommandResult<ZaakType>>
{
    public Guid Id { get; set; }
}
