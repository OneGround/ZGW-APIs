using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Notificaties;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Autorisaties.Web.Handlers;

class DeleteApplicatieCommandHandler
    : AutorisatiesBaseHandler<DeleteApplicatieCommandHandler>,
        IRequestHandler<DeleteApplicatieCommand, CommandResult>
{
    private readonly AcDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;

    public DeleteApplicatieCommandHandler(
        INotificatieService notificatieService,
        IEntityUriService uriService,
        IConfiguration configuration,
        ILogger<DeleteApplicatieCommandHandler> logger,
        IAuthorizationContextAccessor authorizationContextAccessor,
        AcDbContext context,
        ICacheInvalidator cacheInvalidator
    )
        : base(notificatieService, authorizationContextAccessor, uriService, configuration, logger)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<CommandResult> Handle(DeleteApplicatieCommand request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Applicatie>();

        var applicatie = await _context
            .Applicaties.Where(rsinFilter)
            .Include(z => z.Autorisaties)
            .Include(z => z.ClientIds)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (applicatie == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        _context.Applicaties.Remove(applicatie);

        await _cacheInvalidator.InvalidateAsync(CacheEntity.Applicatie, applicatie.ClientIds.Select(client => client.ClientId).ToList());

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Applicatie {Id} successfully deleted.", applicatie.Id);

        await SendNotificationAsync(Actie.destroy, applicatie, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }
}

class DeleteApplicatieCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
