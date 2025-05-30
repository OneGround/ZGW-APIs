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
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

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

        if (!_conceptBusinessRule.ValidateConceptRelation(zaakTypeInformatieObjectType.ZaakType, errors, version: 1.3M))
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
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
