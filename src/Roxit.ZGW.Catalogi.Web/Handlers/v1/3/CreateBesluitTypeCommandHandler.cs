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
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Notificaties;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateBesluitTypeCommandHandler
    : CatalogiBaseHandler<CreateBesluitTypeCommandHandler>,
        IRequestHandler<CreateBesluitTypeCommand, CommandResult<BesluitType>>
{
    private readonly ZtcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBesluitTypeDataService _besluitTypeDataService;

    public CreateBesluitTypeCommandHandler(
        ILogger<CreateBesluitTypeCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        ZtcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IBesluitTypeDataService besluitTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _besluitTypeDataService = besluitTypeDataService;
    }

    public async Task<CommandResult<BesluitType>> Handle(CreateBesluitTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating BesluitType and validating....");

        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
        if (request.ZaakTypen != null && request.ZaakTypen.Any())
        {
            var error = new ValidationError(
                "zaaktypen",
                ErrorCode.Invalid,
                "Toevoegen van zaaktypen aan een besluit is niet meer toegestaan. Maak de relatie aan via het zaaktype."
            );
            return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
        }

        var besluitType = request.BesluitType;

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(c => c.Owner == _rsin);

        var catalogusId = _uriService.GetId(request.Catalogus);
        var catalogus = await _context
            .Catalogussen.Include(c => c.BesluitTypes)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == catalogusId, cancellationToken);

        if (catalogus == null)
        {
            var error = new ValidationError("catalogus", ErrorCode.Invalid, $"Catalogus {request.Catalogus} is onbekend.");
            return new CommandResult<BesluitType>(null, CommandStatus.ValidationError, error);
        }

        await _context.BesluitTypen.AddAsync(besluitType, cancellationToken);

        besluitType.Catalogus = catalogus;
        besluitType.Owner = besluitType.Catalogus.Owner;
        besluitType.Concept = true;

        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does)
        // await AddZaakTypen(request, besluitType, cancellationToken);
        await AddInformatieObjectTypen(request, besluitType, cancellationToken);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<BesluitTypeResponseDto>(besluitType);

            await audittrail.CreatedAsync(besluitType.Catalogus, besluitType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("BesluitType {Id} successfully created.", besluitType.Id);

        // Note: Refresh created BesluitType with all sub-entities within geldigheid which was not loaded
        besluitType = await _besluitTypeDataService.GetAsync(besluitType.Id, cancellationToken: cancellationToken);

        await SendNotificationAsync(Actie.create, besluitType, cancellationToken);

        return new CommandResult<BesluitType>(besluitType, CommandStatus.OK);
    }

    private async Task AddInformatieObjectTypen(CreateBesluitTypeCommand request, BesluitType besluitType, CancellationToken cancellationToken)
    {
        var informatieObjectTypeRsinFilter = GetRsinFilterPredicate<InformatieObjectType>(t => t.Catalogus.Owner == _rsin);
        var informatieObjectTypen = new List<BesluitTypeInformatieObjectType>();

        var now = DateOnly.FromDateTime(DateTime.Now.Date);

        foreach (var (informatieObjectType, index) in request.InformatieObjectTypen.WithIndex())
        {
            var informatieObjectTypenWithinGeldigheid = await _context
                .InformatieObjectTypen.Include(z => z.Catalogus)
                .Where(informatieObjectTypeRsinFilter)
                .Where(i => i.CatalogusId == _uriService.GetId(request.Catalogus))
                .Where(i =>
                    i.Omschrijving == informatieObjectType && now >= i.BeginGeldigheid && (i.EindeGeldigheid == null || now <= i.EindeGeldigheid)
                )
                .ToListAsync(cancellationToken);

            if (informatieObjectTypenWithinGeldigheid.Count == 0)
            {
                _logger.LogInformation(
                    "Waarschuwing: informatieobjecttypen.{index}.omschrijving. InformatieObjectType {informatieObjectType} is onbekend.",
                    index,
                    informatieObjectType
                );
                continue;
            }

            informatieObjectTypen.AddRangeUnique(
                informatieObjectTypenWithinGeldigheid.Select(i => new BesluitTypeInformatieObjectType
                {
                    BesluitType = besluitType,
                    InformatieObjectTypeOmschrijving = i.Omschrijving,
                    Owner = besluitType.Owner,
                    InformatieObjectType = i,
                }),
                (x, y) => x.InformatieObjectTypeOmschrijving == y.InformatieObjectTypeOmschrijving
            );
        }

        _context.BesluitTypeInformatieObjectTypen.AddRange(informatieObjectTypen);

        await _cacheInvalidator.InvalidateAsync(informatieObjectTypen.Select(t => t.InformatieObjectType), besluitType.Catalogus.Owner);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "besluittype" };
}

class CreateBesluitTypeCommand : IRequest<CommandResult<BesluitType>>
{
    public BesluitType BesluitType { get; internal set; }
    public string Catalogus { get; internal set; }
    public IEnumerable<string> ZaakTypen { get; internal set; }
    public IEnumerable<string> InformatieObjectTypen { get; internal set; }
}
