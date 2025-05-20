using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class DeleteZaakTypeInformatieObjectTypeHandler
    : CatalogiBaseHandler<DeleteZaakTypeInformatieObjectTypeHandler>,
        IRequestHandler<DeleteZaakTypeInformatieObjectTypeCommand, CommandResult>
{
    private readonly IConceptBusinessRule _conceptBusinessRule;
    private readonly ZtcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ICacheInvalidator _cacheInvalidator;

    public DeleteZaakTypeInformatieObjectTypeHandler(
        ILogger<DeleteZaakTypeInformatieObjectTypeHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IConceptBusinessRule conceptBusinessRule,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory,
        ICacheInvalidator cacheInvalidator
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _conceptBusinessRule = conceptBusinessRule;
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<CommandResult> Handle(DeleteZaakTypeInformatieObjectTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakTypeInformatieObjectType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var zaakTypeInformatieObjectType = await _context
            .ZaakTypeInformatieObjectTypen.Where(rsinFilter)
            .Include(z => z.ZaakType.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakTypeInformatieObjectType == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        var informatieObjectTypen = await _context
            .InformatieObjectTypen.AsNoTracking()
            .Where(i => i.Catalogus.Owner == _rsin)
            .Where(i => i.Omschrijving == zaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving)
            .ToListAsync(cancellationToken); // Note: Matching on omschrijving can have multiple results within the same catalog (the same omschrijving but in different versions)
        if (informatieObjectTypen.Count == 0)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        _conceptBusinessRule.ValidateConceptRelation(zaakTypeInformatieObjectType.ZaakType, errors, version: 1.0M);
        foreach (var informatieObjectType in informatieObjectTypen)
        {
            _conceptBusinessRule.ValidateConceptRelation(informatieObjectType, errors, version: 1.0M);
        }

        if (errors.Count >= 1)
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.Last());
        }

        _logger.LogDebug("Deleting ZaakTypeInformatieObjectType {Id}....", zaakTypeInformatieObjectType.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakTypeInformatieObjectTypeResponseDto>(zaakTypeInformatieObjectType);

            _context.ZaakTypeInformatieObjectTypen.Remove(zaakTypeInformatieObjectType);

            await audittrail.DestroyedAsync(zaakTypeInformatieObjectType.ZaakType, zaakTypeInformatieObjectType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _cacheInvalidator.InvalidateAsync(zaakTypeInformatieObjectType.ZaakType);

        _logger.LogDebug("ZaakTypeInformatieObjectType {Id} successfully deleted.", zaakTypeInformatieObjectType.Id);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype-informatieobjecttypen" };
}

class DeleteZaakTypeInformatieObjectTypeCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
