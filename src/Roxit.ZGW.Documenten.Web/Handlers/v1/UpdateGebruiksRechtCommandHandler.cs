using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Contracts.v1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.Notificaties;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

class UpdateGebruiksRechtCommandHandler
    : DocumentenBaseHandler<UpdateGebruiksRechtCommandHandler>,
        IRequestHandler<UpdateGebruiksRechtCommand, CommandResult<GebruiksRecht>>
{
    private readonly DrcDbContext _context;
    private readonly IEntityUpdater<GebruiksRecht> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateGebruiksRechtCommandHandler(
        ILogger<UpdateGebruiksRechtCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IEntityUpdater<GebruiksRecht> entityUpdater,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<GebruiksRecht>> Handle(UpdateGebruiksRechtCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get GebruiksRecht {Id} and validating....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<GebruiksRecht>(o => o.InformatieObject.Owner == _rsin);

        var gebruiksRecht = await _context
            .GebruiksRechten.Include(z => z.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (gebruiksRecht == null)
        {
            return new CommandResult<GebruiksRecht>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(gebruiksRecht.InformatieObject, AuthorizationScopes.Documenten.Update))
        {
            return new CommandResult<GebruiksRecht>(null, CommandStatus.Forbidden);
        }

        var informatieObjectUrl = _uriService.GetUri(gebruiksRecht.InformatieObject);

        _logger.LogDebug("Updating GebruiksRecht {Id}....", gebruiksRecht.Id);

        if (request.InformatieObject != informatieObjectUrl)
        {
            var informatieObjectRsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();
            var informatieObject = await _context
                .EnkelvoudigInformatieObjecten.Where(informatieObjectRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.InformatieObject), cancellationToken);

            if (informatieObject == null)
            {
                string error = "Ongeldige hyperlink - Object bestaat niet.";
                return new CommandResult<GebruiksRecht>(
                    null,
                    CommandStatus.ValidationError,
                    new ValidationError("informatieobject", ErrorCode.DoesNotExist, error)
                );
            }

            gebruiksRecht.InformatieObject = informatieObject;
            gebruiksRecht.InformatieObjectId = informatieObject.Id;
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<GebruiksRechtResponseDto>(gebruiksRecht);

            _entityUpdater.Update(request.GebruiksRecht, gebruiksRecht);

            audittrail.SetNew<GebruiksRechtResponseDto>(gebruiksRecht);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(gebruiksRecht.InformatieObject, gebruiksRecht, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(gebruiksRecht.InformatieObject, gebruiksRecht, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("GebruiksRecht {Id} successfully updated.", gebruiksRecht.Id);
        }

        await SendNotificationAsync(Actie.update, gebruiksRecht, cancellationToken);

        return new CommandResult<GebruiksRecht>(gebruiksRecht, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" };
}

class UpdateGebruiksRechtCommand : IRequest<CommandResult<GebruiksRecht>>
{
    public GebruiksRecht GebruiksRecht { get; internal set; }
    public Guid Id { get; internal set; }
    public string InformatieObject { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
