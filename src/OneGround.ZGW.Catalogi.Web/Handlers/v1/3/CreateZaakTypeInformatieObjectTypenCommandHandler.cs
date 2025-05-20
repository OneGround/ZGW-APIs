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
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class CreateZaakTypeInformatieObjectTypenCommandHandler
    : CatalogiBaseHandler<CreateZaakTypeInformatieObjectTypenCommandHandler>,
        IRequestHandler<CreateZaakTypeInformatieObjectTypenCommand, CommandResult<ZaakTypeInformatieObjectType>>
{
    private readonly ZtcDbContext _context;
    private readonly IZaakTypeInformatieObjectTypenBusinessRuleService _businessRuleService;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CreateZaakTypeInformatieObjectTypenCommandHandler(
        ILogger<CreateZaakTypeInformatieObjectTypenCommandHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        ZtcDbContext context,
        IZaakTypeInformatieObjectTypenBusinessRuleService businessRuleService,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IAuditTrailFactory auditTrailFactory,
        ICacheInvalidator cacheInvalidator
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _businessRuleService = businessRuleService;
        _auditTrailFactory = auditTrailFactory;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<CommandResult<ZaakTypeInformatieObjectType>> Handle(
        CreateZaakTypeInformatieObjectTypenCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Creating ZaakTypeInformatieObjectTypen and validating....");

        var zaakTypeInformatieObjectType = request.ZaakTypeInformatieObjectType;

        var errors = new List<ValidationError>();

        var zaakTypeFilter = GetRsinFilterPredicate<ZaakType>(t => t.Catalogus.Owner == _rsin);
        var statusTypeFilter = GetRsinFilterPredicate<StatusType>(t => t.ZaakType.Catalogus.Owner == _rsin);

        var zaakType = await _context
            .ZaakTypen.Where(zaakTypeFilter)
            .Include(z => z.Catalogus)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakType), cancellationToken);

        if (zaakType == null)
        {
            var error = new ValidationError("zaaktype", ErrorCode.NotFound, $"Zaaktype '{request.ZaakType}' niet gevonden.");
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.NotFound, error);
        }

        StatusType statusType = null;
        if (request.StatusType != null)
        {
            statusType = await _context
                .StatusTypen.Include(s => s.ZaakType)
                .Where(statusTypeFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.StatusType), cancellationToken);

            if (statusType == null)
            {
                var error = new ValidationError("statusType", ErrorCode.NotFound, $"StatusType '{request.StatusType}' niet gevonden.");

                errors.Add(error);
            }
            else if (statusType.ZaakType.Url != zaakType.Url)
            {
                var error = new ValidationError(
                    "statusType",
                    ErrorCode.Invalid,
                    $"StatusType '{request.StatusType}' belongs not to the specified zaaktype of the request."
                );

                errors.Add(error);
            }

            if (errors.Count != 0)
            {
                return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }

        if (
            !await _businessRuleService.ValidateExistsAsync(
                zaakType.Id,
                request.ZaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving,
                request.ZaakTypeInformatieObjectType.VolgNummer,
                request.ZaakTypeInformatieObjectType.Richting,
                errors
            )
        )
        {
            return new CommandResult<ZaakTypeInformatieObjectType>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        await _context.ZaakTypeInformatieObjectTypen.AddAsync(zaakTypeInformatieObjectType, cancellationToken);

        zaakTypeInformatieObjectType.Id = Guid.NewGuid();
        zaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving = request.ZaakTypeInformatieObjectType.InformatieObjectTypeOmschrijving;
        zaakTypeInformatieObjectType.StatusType = statusType;
        zaakTypeInformatieObjectType.ZaakType = zaakType;
        zaakTypeInformatieObjectType.Owner = zaakTypeInformatieObjectType.ZaakType.Owner;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<ZaakTypeInformatieObjectTypeResponseDto>(zaakTypeInformatieObjectType);

            await audittrail.CreatedAsync(zaakTypeInformatieObjectType.ZaakType, zaakTypeInformatieObjectType, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _cacheInvalidator.InvalidateAsync(zaakTypeInformatieObjectType.ZaakType);

        _logger.LogDebug("ZaakTypeInformatieObjectTypen {Id} successfully created.", zaakTypeInformatieObjectType.Id);

        return new CommandResult<ZaakTypeInformatieObjectType>(zaakTypeInformatieObjectType, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.ZTC, Resource = "zaaktype-informatieobjecttypen" };
}

class CreateZaakTypeInformatieObjectTypenCommand : IRequest<CommandResult<ZaakTypeInformatieObjectType>>
{
    public ZaakTypeInformatieObjectType ZaakTypeInformatieObjectType { get; set; }
    public string ZaakType { get; set; }
    public string StatusType { get; set; }
}
