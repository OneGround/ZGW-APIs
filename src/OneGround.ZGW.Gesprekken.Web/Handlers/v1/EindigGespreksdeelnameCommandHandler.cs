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

class EindigGespreksdeelnameCommandHandler
    : GesprekkenBaseHandler<EindigGespreksdeelnameCommandHandler>,
        IRequestHandler<EindigGespreksdeelnameCommand, CommandResult<Gespreksdeelname>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;

    public EindigGespreksdeelnameCommandHandler(
        ILogger<EindigGespreksdeelnameCommandHandler> logger,
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

    public async Task<CommandResult<Gespreksdeelname>> Handle(EindigGespreksdeelnameCommand request, CancellationToken cancellationToken)
    {
        var gesprekId = await _context
            .Gespreksdeelnames.Where(d => d.Owner == _rsin && d.Id == request.Id)
            .Select(d => (Guid?)d.GesprekId)
            .SingleOrDefaultAsync(cancellationToken);
        if (gesprekId == null)
        {
            return new CommandResult<Gespreksdeelname>(null, CommandStatus.NotFound);
        }

        var gesprek = await _context.LoadWithDeelnamesAsync(_rsin, gesprekId.Value, cancellationToken);
        var deelname = gesprek.Deelnames.Single(d => d.Id == request.Id);

        if (!deelname.IsActief)
        {
            return new CommandResult<Gespreksdeelname>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("nonFieldErrors", ErrorCodes.AlBeeindigd, "De deelname is al beëindigd.")
            );
        }

        deelname.MomentEindeDeelname = DateTime.UtcNow;
        _eventLog.Append(gesprek, EventTypes.GespreksdeelnameBeeindigd, deelname);
        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<Gespreksdeelname>(deelname, CommandStatus.OK);
    }
}

class EindigGespreksdeelnameCommand : IRequest<CommandResult<Gespreksdeelname>>
{
    public Guid Id { get; set; }
}
