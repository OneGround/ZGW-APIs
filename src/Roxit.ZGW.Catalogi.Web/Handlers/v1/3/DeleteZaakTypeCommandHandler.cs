using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class DeleteZaakTypeCommandHandler : CatalogiBaseHandler<DeleteZaakTypeCommandHandler>, IRequestHandler<DeleteZaakTypeCommand, CommandResult>
{
    private readonly ZtcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZaakTypeDataService _zaakTypeDataService;

    public DeleteZaakTypeCommandHandler(
        ILogger<DeleteZaakTypeCommandHandler> logger,
        IConfiguration configuration,
        ZtcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICacheInvalidator cacheInvalidator,
        IAuditTrailFactory auditTrailFactory,
        IZaakTypeDataService zaakTypeDataService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _auditTrailFactory = auditTrailFactory;
        _zaakTypeDataService = zaakTypeDataService;
    }

    public async Task<CommandResult> Handle(DeleteZaakTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakType {Id}....", request.Id);

        var zaakType = await _zaakTypeDataService.GetAsync(request.Id, trackingChanges: true, cancellationToken: cancellationToken);
        if (zaakType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!zaakType.Concept)
        {
            return new CommandResult<ZaakType>(
                null,
                CommandStatus.ValidationError,
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.NonConceptObject,
                    "Het is niet toegestaan om een non-concept zaaktype te verwijderen."
                )
            );
        }

        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501

        //var errors = new List<ValidationError>();

        //if (await _context.StatusTypen
        //    .Where(GetRsinFilterPredicate<StatusType>())
        //    .AnyAsync(z => z.ZaakObjectType.ZaakTypeId == zaakType.Id, cancellationToken))
        //{
        //    errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Kan ZaakType niet verwijderen omdat dit ZaakType refereert naar ander ZaakType.StatusType.ZaakObjectType."));
        //}

        //if (await _context.ResultaatTypen
        //    .Where(GetRsinFilterPredicate<ResultaatType>())
        //    .AnyAsync(z => z.ZaakObjectType.ZaakTypeId == zaakType.Id, cancellationToken))
        //{
        //    errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Kan ZaakType niet verwijderen omdat dit ZaakType refereert naar ander ZaakType.ResultaatType.ZaakObjectType."));
        //}

        //if (errors.Any())
        //{
        //    return new CommandResult<ZaakType>(null, CommandStatus.ValidationError, errors.ToArray());
        //}
        // ----

        _logger.LogDebug("Deleting ZaakType {Id}....", zaakType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeResponseDto>(zaakType);

            // TODO: Created separate ticket for:
            //   https://support.roxit.nl/jira/browse/FUND-1414 ZGW: Deleting entities should check if reverse relations exist, if so delete them first

            // Delete resultaattype relations holding zaaktype
            //var zaaktypeResultaatTypen = await _context.ResultaatTypen
            //    .Where(GetRsinFilterPredicate<ResultaatType>())
            //    .Where(z => z.ZaakTypeId == zaakType.Id)
            //    .ToListAsync(cancellationToken);
            //_context.ResultaatTypen.RemoveRange(zaaktypeResultaatTypen);

            //var zaaktypeStatusTypen = await _context.StatusTypen
            //    .Where(GetRsinFilterPredicate<StatusType>())
            //    .Where(z => z.ZaakTypeId == zaakType.Id)
            //    .ToListAsync(cancellationToken);
            //_context.StatusTypen.RemoveRange(zaaktypeStatusTypen);
            // ----

            _context.ZaakTypen.Remove(zaakType);

            await _cacheInvalidator.InvalidateAsync(zaakType.ZaakObjectTypen, zaakType.Catalogus.Owner);
            // TODO: We ask VNG how the relations can be edited:
            //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
            //await _cacheInvalidator.InvalidateAsync(zaakType.StatusTypen, zaakType.Catalogus.Owner);
            //await _cacheInvalidator.InvalidateAsync(zaakType.ResultaatTypen, zaakType.Catalogus.Owner);
            // ----
            await _cacheInvalidator.InvalidateAsync(
                zaakType.ZaakTypeInformatieObjectTypen.Select(t => t.InformatieObjectType),
                zaakType.Catalogus.Owner
            );
            await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeBesluitTypen.Select(t => t.BesluitType), zaakType.Catalogus.Owner);
            await _cacheInvalidator.InvalidateAsync(zaakType.ZaakTypeDeelZaakTypen.Select(z => z.DeelZaakType), zaakType.Catalogus.Owner);
            await _cacheInvalidator.InvalidateAsync(
                zaakType.ZaakTypeGerelateerdeZaakTypen.Select(z => z.GerelateerdeZaakType),
                zaakType.Catalogus.Owner
            );
            await _cacheInvalidator.InvalidateAsync(zaakType);

            await audittrail.DestroyedAsync(zaakType.Catalogus, zaakType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakType {Id} successfully deleted.", zaakType.Id);

        await SendNotificationAsync(Actie.destroy, zaakType, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype" };
}

class DeleteZaakTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
