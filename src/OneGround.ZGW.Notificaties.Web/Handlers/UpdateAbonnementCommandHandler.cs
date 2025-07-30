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

namespace OneGround.ZGW.Notificaties.Web.Handlers;

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

        foreach (var abonnementkanaal in request.Abonnement.AbonnementKanalen)
        {
            var kanaal = await _context.Kanalen.SingleOrDefaultAsync(k => k.Naam == abonnementkanaal.Kanaal.Naam, cancellationToken);

            if (kanaal == null)
            {
                var error = new ValidationError(
                    "identificatie",
                    ErrorCode.NotFound,
                    $"In het abonnement is een niet bestaand kanaal '{abonnementkanaal.Kanaal.Naam}' opgegeven."
                );
                return new CommandResult<Abonnement>(null, CommandStatus.ValidationError, error);
            }
            else
            {
                var errors = new List<ValidationError>();

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

                if (errors.Count != 0)
                {
                    return new CommandResult<Abonnement>(null, CommandStatus.ValidationError, errors.ToArray());
                }
            }

            abonnementkanaal.Kanaal = kanaal;
            abonnement.AbonnementKanalen.Add(abonnementkanaal);
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
