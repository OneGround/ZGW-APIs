using System;
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
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;
using OneGround.ZGW.Gesprekken.Web.Events;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

/// <summary>
/// Registers that a gespreksdeelname has read a gespreksbijdrage. Registering twice returns the existing registration,
/// so clients can call this every time they show the bijdrage.
/// </summary>
class MarkeerGelezenCommandHandler
    : GesprekkenBaseHandler<MarkeerGelezenCommandHandler>,
        IRequestHandler<MarkeerGelezenCommand, CommandResult<GelezenGespreksbijdrage>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;

    public MarkeerGelezenCommandHandler(
        ILogger<MarkeerGelezenCommandHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IGesprekEventLog eventLog
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _eventLog = eventLog;
    }

    public async Task<CommandResult<GelezenGespreksbijdrage>> Handle(MarkeerGelezenCommand request, CancellationToken cancellationToken)
    {
        var bijdrage = await _context
            .Gespreksbijdragen.Where(b => b.Owner == _rsin)
            .SingleOrDefaultAsync(b => b.Id == request.GespreksbijdrageId, cancellationToken);
        if (bijdrage == null)
        {
            return new CommandResult<GelezenGespreksbijdrage>(null, CommandStatus.NotFound);
        }

        var gesprek = await _context.LoadWithDeelnamesAsync(_rsin, bijdrage.GesprekId, cancellationToken);
        if (gesprek.Deelnames.All(d => d.Id != request.GespreksdeelnameId))
        {
            return new CommandResult<GelezenGespreksbijdrage>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("gespreksdeelname", ErrorCodes.DeelnameNietInGesprek, "De gespreksdeelname hoort niet bij dit gesprek.")
            );
        }

        var bestaand = await _context.GelezenGespreksbijdragen.SingleOrDefaultAsync(
            g => g.GespreksbijdrageId == bijdrage.Id && g.GespreksdeelnameId == request.GespreksdeelnameId,
            cancellationToken
        );
        if (bestaand != null)
        {
            return new CommandResult<GelezenGespreksbijdrage>(bestaand, CommandStatus.OK);
        }

        var gelezen = new GelezenGespreksbijdrage
        {
            Owner = _rsin,
            GespreksbijdrageId = bijdrage.Id,
            GespreksdeelnameId = request.GespreksdeelnameId,
            MomentGelezen = DateTime.UtcNow,
        };

        await _context.GelezenGespreksbijdragen.AddAsync(gelezen, cancellationToken);
        _eventLog.Append(gesprek, EventTypes.GespreksbijdrageGelezen, gelezen);
        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<GelezenGespreksbijdrage>(gelezen, CommandStatus.OK);
    }
}

class MarkeerGelezenCommand : IRequest<CommandResult<GelezenGespreksbijdrage>>
{
    public Guid GespreksbijdrageId { get; set; }
    public Guid GespreksdeelnameId { get; set; }
}
