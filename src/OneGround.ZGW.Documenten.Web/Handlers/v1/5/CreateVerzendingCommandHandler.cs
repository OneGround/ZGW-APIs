using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1._5;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

class CreateVerzendingCommandHandler
    : DocumentenBaseHandler<CreateVerzendingCommandHandler>,
        IRequestHandler<CreateVerzendingCommand, CommandResult<Verzending>>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IVerzendingBusinessRuleService _verzendingBusinessRuleService;

    public CreateVerzendingCommandHandler(
        ILogger<CreateVerzendingCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IVerzendingBusinessRuleService verzendingBusinessRuleService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _verzendingBusinessRuleService = verzendingBusinessRuleService;
    }

    public async Task<CommandResult<Verzending>> Handle(CreateVerzendingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating Verzending....");

        var verzending = request.Verzending;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var informatieObjectId = _uriService.GetId(request.InformatieObjectUrl);
        var informatieObject = await _context
            .EnkelvoudigInformatieObjecten.Include(e => e.Verzendingen)
            .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(e => e.Id == informatieObjectId, cancellationToken);

        if (informatieObject == null)
        {
            return new CommandResult<Verzending>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("informatieobject", ErrorCode.Invalid, $"InformatieObject {request.InformatieObjectUrl} is onbekend.")
            );
        }

        if (
            !_authorizationContext.IsAuthorized(
                informatieObject.InformatieObjectType,
                informatieObject.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding,
                AuthorizationScopes.Documenten.Update
            )
        )
        {
            return new CommandResult<Verzending>(null, CommandStatus.Forbidden);
        }

        _verzendingBusinessRuleService.Validate(informatieObject, request.Verzending, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<Verzending>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        await _context.Verzendingen.AddAsync(verzending, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

        verzending.InformatieObjectId = informatieObject.Id;
        verzending.InformatieObject = informatieObject;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<VerzendingResponseDto>(verzending);

            await audittrail.CreatedAsync(verzending.InformatieObject, verzending, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Verzending {verzendingId} successfully created.", verzending.Id);
        }

        await SendNotificationAsync(Actie.create, verzending, cancellationToken);

        return new CommandResult<Verzending>(verzending, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "Verzending" };
}

class CreateVerzendingCommand : IRequest<CommandResult<Verzending>>
{
    public Verzending Verzending { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
}
