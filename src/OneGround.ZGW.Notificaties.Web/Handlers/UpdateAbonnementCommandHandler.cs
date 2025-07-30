using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.Validators;

namespace OneGround.ZGW.Notificaties.Web.Handlers;

class UpdateAbonnementCommandHandler : ZGWBaseHandler, IRequestHandler<UpdateAbonnementCommand, CommandResult<Abonnement>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<UpdateAbonnementCommandHandler> _logger;
    private readonly IAbonnementKanaalValidator _abonnementKanaalValidator;

    public UpdateAbonnementCommandHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<UpdateAbonnementCommandHandler> logger,
        IAbonnementKanaalValidator abonnementKanaalValidator
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
        _abonnementKanaalValidator = abonnementKanaalValidator;
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

        var errors = new List<ValidationError>();

        foreach (var abonnementkanaal in request.Abonnement.AbonnementKanalen)
        {
            var kanaal = await _context.Kanalen.SingleOrDefaultAsync(k => k.Naam == abonnementkanaal.Kanaal.Naam, cancellationToken);
            if (kanaal == null)
            {
                var error = new ValidationError(
                    "naam",
                    ErrorCode.NotFound,
                    $"In het abonnement is een niet bestaand kanaal '{abonnementkanaal.Kanaal.Naam}' opgegeven."
                );
                errors.Add(error);
            }
            else
            {
                if (!_abonnementKanaalValidator.Validate(abonnementkanaal, kanaal, errors))
                {
                    continue;
                }
            }

            abonnementkanaal.Kanaal = kanaal;
            abonnement.AbonnementKanalen.Add(abonnementkanaal);
        }

        if (errors.Any())
        {
            return new CommandResult<Abonnement>(null, CommandStatus.ValidationError, errors.ToArray());
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
