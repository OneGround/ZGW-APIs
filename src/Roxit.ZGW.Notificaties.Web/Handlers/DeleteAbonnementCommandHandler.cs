using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Web.Handlers;

class DeleteAbonnementCommandHandler : ZGWBaseHandler, IRequestHandler<DeleteAbonnementCommand, CommandResult>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<DeleteAbonnementCommandHandler> _logger;

    public DeleteAbonnementCommandHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<DeleteAbonnementCommandHandler> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CommandResult> Handle(DeleteAbonnementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Abonnement {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Abonnement>();

        var abonnement = await _context.Abonnementen.Where(rsinFilter).SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (abonnement == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        _logger.LogDebug("Deleting Abonnement {Id}....", abonnement.Id);

        _context.Abonnementen.Remove(abonnement);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Abonnement {Id} successfully deleted.", abonnement.Id);

        return new CommandResult(CommandStatus.OK);
    }
}

class DeleteAbonnementCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
