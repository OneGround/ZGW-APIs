using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Web.Handlers;

class UpdateAbonnementCommandHandler : ZGWBaseHandler, IRequestHandler<UpdateAbonnementCommand, CommandResult<Abonnement>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<UpdateAbonnementCommandHandler> _logger;

    public UpdateAbonnementCommandHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<UpdateAbonnementCommandHandler> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CommandResult<Abonnement>> Handle(UpdateAbonnementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Abonnement {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Abonnement>();

        var abonnement = await _context
            .Abonnementen.Where(rsinFilter)
            .Include(a => a.AbonnementKanalen)
            .SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (abonnement == null)
        {
            return new CommandResult<Abonnement>(null, CommandStatus.NotFound);
        }

        // Clear old AbonnementKanalen...
        abonnement.AbonnementKanalen.Clear();

        _logger.LogDebug("Updating Abonnement {Id}....", abonnement.Id);

        foreach (var ak in request.Abonnement.AbonnementKanalen)
        {
            var kanaal = await _context.Kanalen.SingleOrDefaultAsync(k => k.Naam == ak.Kanaal.Naam, cancellationToken);

            if (kanaal == null)
            {
                var error = new ValidationError(
                    "identificatie",
                    ErrorCode.NotFound,
                    $"In het abonnement is een niet bestaand kanaal '{ak.Kanaal.Naam}' opgegeven."
                );
                return new CommandResult<Abonnement>(null, CommandStatus.ValidationError, error);
            }

            ak.Kanaal = kanaal;
            abonnement.AbonnementKanalen.Add(ak);
        }

        abonnement.Auth = request.Abonnement.Auth;
        abonnement.CallbackUrl = request.Abonnement.CallbackUrl;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Abonnement {Id} successfully updated.", abonnement.Id);

        return new CommandResult<Abonnement>(abonnement, CommandStatus.OK);
    }
}

class UpdateAbonnementCommand : IRequest<CommandResult<Abonnement>>
{
    public Abonnement Abonnement { get; internal set; }
    public Guid Id { get; internal set; }
}
