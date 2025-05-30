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
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

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
        _logger.LogDebug("Get ZaakType {Id}....", request.Id);

        var zaakType = await _context
            .ZaakTypen.AsSplitQuery()
            .Include(z => z.Catalogus)
            .Include(z => z.ZaakTypeBesluitTypen)
            .Include(z => z.ZaakTypeInformatieObjectTypen)
            .Include(z => z.ZaakTypeDeelZaakTypen)
            .Include(z => z.ZaakTypeGerelateerdeZaakTypen)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);
        if (zaakType == null)
        {
            return new CommandResult<ZaakType>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        var zaakTypeBesluitTypen = await _context
            .BesluitTypen.Include(b => b.Catalogus)
            .Where(GetRsinFilterPredicate<BesluitType>(o => o.Catalogus.Owner == _rsin))
            .Where(b => zaakType.ZaakTypeBesluitTypen.Select(b => b.BesluitTypeOmschrijving).Any(o => b.Omschrijving == o))
            .ToListAsync(cancellationToken);

        foreach (var besluitType in zaakTypeBesluitTypen)
        {
            if (!_conceptBusinessRule.ValidateNonConceptRelation(besluitType, errors))
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        var zaakTypeInformatieObjectTypen = await _context
            .InformatieObjectTypen.Include(i => i.Catalogus)
            .Where(GetRsinFilterPredicate<InformatieObjectType>(o => o.Catalogus.Owner == _rsin))
            .Where(i => zaakType.ZaakTypeInformatieObjectTypen.Select(i => i.InformatieObjectTypeOmschrijving).Any(o => i.Omschrijving == o))
            .ToListAsync(cancellationToken);

        foreach (var informatieObjectType in zaakTypeInformatieObjectTypen)
        {
            if (!_conceptBusinessRule.ValidateNonConceptRelation(informatieObjectType, errors))
            {
                return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        _logger.LogDebug("Publishing ZaakType {Id}....", zaakType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeResponseDto>(zaakType);

            zaakType.Concept = false;

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
