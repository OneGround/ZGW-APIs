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
using OneGround.ZGW.Gesprekken.Web.Afsluiten;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

class EindigGesprekCommandHandler : GesprekkenBaseHandler<EindigGesprekCommandHandler>, IRequestHandler<EindigGesprekCommand, CommandResult<Gesprek>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekAfsluiter _afsluiter;

    public EindigGesprekCommandHandler(
        ILogger<EindigGesprekCommandHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IGesprekAfsluiter afsluiter
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _afsluiter = afsluiter;
    }

    public async Task<CommandResult<Gesprek>> Handle(EindigGesprekCommand request, CancellationToken cancellationToken)
    {
        var gesprek = await _context.LoadWithDeelnamesAsync(_rsin, request.Id, cancellationToken);
        if (gesprek == null)
        {
            return new CommandResult<Gesprek>(null, CommandStatus.NotFound);
        }

        if (gesprek.MomentEindeGesprek.HasValue)
        {
            return new CommandResult<Gesprek>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("nonFieldErrors", ErrorCodes.AlBeeindigd, "Het gesprek is al beëindigd.")
            );
        }

        await _afsluiter.SluitAsync(gesprek, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<Gesprek>(gesprek, CommandStatus.OK);
    }
}

class EindigGesprekCommand : IRequest<CommandResult<Gesprek>>
{
    public Guid Id { get; set; }
}
