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

namespace OneGround.ZGW.Notificaties.Web.Handlers;

class CreateAbonnementCommandHandler : ZGWBaseHandler, IRequestHandler<CreateAbonnementCommand, CommandResult<Abonnement>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<CreateAbonnementCommandHandler> _logger;

    public CreateAbonnementCommandHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<CreateAbonnementCommandHandler> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
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
                errors.Add(
                    new ValidationError(
                        "kanaal",
                        ErrorCode.NotFound,
                        $"In het abonnement is een niet bestaand kanaal '{abonnementkanaal.Kanaal.Naam}' opgegeven."
                    )
                );
            }
            else
            {
                var kanaalFilterMap = kanaal.Filters.ToHashSet();

                foreach (var filter in abonnementkanaal.Filters)
                {
                    if (filter.Key == "#resource")
                    {
                        continue;
                    }
                    if (filter.Key == "#actie")
                    {
                        string[] acties = ["create", "update", "destroy"];

                        if (!acties.Contains(filter.Value)) // TODO: Consider using a business-rule service (so shared with create/modify)
                        {
                            errors.Add(
                                new ValidationError(
                                    "filter",
                                    ErrorCode.NotFound,
                                    $"In het abonnement is bij filter '#actie' een incorrecte waarde '{filter.Value}' opgegeven."
                                )
                            );
                        }
                    }
                    else if (!kanaalFilterMap.Contains(filter.Key))
                    {
                        errors.Add(
                            new ValidationError(
                                "filter",
                                ErrorCode.NotFound,
                                $"In het abonnement is een niet bestaand filter '{filter.Key}' opgegeven."
                            )
                        );
                    }
                }
            }
            abonnementkanaal.Kanaal = kanaal;
        }

        if (errors.Count != 0)
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
