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

class CreateKanaalCommandHandler : ZGWBaseHandler, IRequestHandler<CreateKanaalCommand, CommandResult<Kanaal>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<CreateKanaalCommandHandler> _logger;

    public CreateKanaalCommandHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<CreateKanaalCommandHandler> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CommandResult<Kanaal>> Handle(CreateKanaalCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating Kanaal....");

        var kanaal = request.Kanaal;

        var naamExists = await _context.Kanalen.AnyAsync(k => k.Naam == kanaal.Naam, cancellationToken);
        if (naamExists)
        {
            var error = new ValidationError("naam", ErrorCode.Unique, "Er bestaat al een kanaal met eenzelfde Naam.");

            return new CommandResult<Kanaal>(null, CommandStatus.ValidationError, error);
        }

        await _context.Kanalen.AddAsync(kanaal, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<Kanaal>(kanaal, CommandStatus.OK);
    }
}

class CreateKanaalCommand : IRequest<CommandResult<Kanaal>>
{
    public Kanaal Kanaal { get; internal set; }
}
