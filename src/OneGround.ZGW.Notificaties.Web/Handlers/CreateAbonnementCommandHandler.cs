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

class CreateAbonnementCommandHandler : ZGWBaseHandler, IRequestHandler<CreateAbonnementCommand, CommandResult<Abonnement>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<CreateAbonnementCommandHandler> _logger;
    private readonly IAbonnementKanaalValidator _abonnementKanaalValidator;

    public CreateAbonnementCommandHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<CreateAbonnementCommandHandler> logger,
        IAbonnementKanaalValidator abonnementKanaalValidator
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
        _abonnementKanaalValidator = abonnementKanaalValidator;
    }

    public async Task<CommandResult<Abonnement>> Handle(CreateAbonnementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating Abonnement....");

        var abonnement = request.Abonnement;

        var errors = new List<ValidationError>();

        foreach (var abonnementkanaal in abonnement.AbonnementKanalen)
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
        }

        if (errors.Any())
        {
            return new CommandResult<Abonnement>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        abonnement.Owner = _rsin;

        await _context.Abonnementen.AddAsync(abonnement, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<Abonnement>(abonnement, CommandStatus.OK);
    }
}

class CreateAbonnementCommand : IRequest<CommandResult<Abonnement>>
{
    public Abonnement Abonnement { get; internal set; }
}
