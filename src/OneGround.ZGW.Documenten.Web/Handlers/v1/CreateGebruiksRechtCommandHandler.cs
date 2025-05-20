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
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class CreateGebruiksRechtCommandHandler
    : DocumentenBaseHandler<CreateGebruiksRechtCommandHandler>,
        IRequestHandler<CreateGebruiksRechtCommand, CommandResult<GebruiksRecht>>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateGebruiksRechtCommandHandler(
        ILogger<CreateGebruiksRechtCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<GebruiksRecht>> Handle(CreateGebruiksRechtCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating GebruiksRecht....");

        var gebruiksRecht = request.GebruiksRecht;
        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();
        var informatieObjectId = _uriService.GetId(request.InformatieObjectUrl);

        var informatieObject = await _context
            .EnkelvoudigInformatieObjecten.Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(e => e.Id == informatieObjectId, cancellationToken);

        if (informatieObject == null)
        {
            return new CommandResult<GebruiksRecht>(
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
            return new CommandResult<GebruiksRecht>(null, CommandStatus.Forbidden);
        }

        await _context.GebruiksRechten.AddAsync(gebruiksRecht, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

        gebruiksRecht.InformatieObjectId = informatieObject.Id;
        gebruiksRecht.InformatieObject = informatieObject;

        informatieObject.IndicatieGebruiksrecht = true; // Note: Gebruiksrechten op informatieobjecten (drc-006)

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<GebruiksRechtResponseDto>(gebruiksRecht);

            await audittrail.CreatedAsync(gebruiksRecht.InformatieObject, gebruiksRecht, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("GebruiksRecht {Id} successfully created.", gebruiksRecht.Id);
        }

        await SendNotificationAsync(Actie.create, gebruiksRecht, cancellationToken);

        return new CommandResult<GebruiksRecht>(gebruiksRecht, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" };
}

class CreateGebruiksRechtCommand : IRequest<CommandResult<GebruiksRecht>>
{
    public GebruiksRecht GebruiksRecht { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
}
