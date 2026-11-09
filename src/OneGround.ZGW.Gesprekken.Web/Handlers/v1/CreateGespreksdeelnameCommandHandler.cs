using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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

class CreateGespreksdeelnameCommandHandler
    : GesprekkenBaseHandler<CreateGespreksdeelnameCommandHandler>,
        IRequestHandler<CreateGespreksdeelnameCommand, CommandResult<Gespreksdeelname>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;

    public CreateGespreksdeelnameCommandHandler(
        ILogger<CreateGespreksdeelnameCommandHandler> logger,
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

    public async Task<CommandResult<Gespreksdeelname>> Handle(CreateGespreksdeelnameCommand request, CancellationToken cancellationToken)
    {
        var deelname = request.Deelname;

        var gesprek = await _context.LoadWithDeelnamesAsync(_rsin, deelname.GesprekId, cancellationToken);
        if (gesprek == null)
        {
            return Invalid("gesprek", ErrorCode.NotFound, "Het gesprek bestaat niet.");
        }

        if (gesprek.MomentEindeGesprek.HasValue)
        {
            return Invalid("gesprek", ErrorCodes.GesprekBeeindigd, "Aan een beëindigd gesprek kan geen deelname worden toegevoegd.");
        }

        deelname.Owner = _rsin;
        deelname.AanvangsmomentDeelname = DateTime.UtcNow;
        gesprek.Deelnames.Add(deelname);
        _eventLog.Append(gesprek, EventTypes.GespreksdeelnameAangemaakt, deelname);

        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<Gespreksdeelname>(deelname, CommandStatus.OK);
    }

    private static CommandResult<Gespreksdeelname> Invalid(string name, string code, string reason) =>
        new(null, CommandStatus.ValidationError, new ValidationError(name, code, reason));
}

class CreateGespreksdeelnameCommand : IRequest<CommandResult<Gespreksdeelname>>
{
    public Gespreksdeelname Deelname { get; internal set; }
}
