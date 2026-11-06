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
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Bestanden;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;
using OneGround.ZGW.Gesprekken.Web.Events;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

class CreateGespreksbijdrageCommandHandler
    : GesprekkenBaseHandler<CreateGespreksbijdrageCommandHandler>,
        IRequestHandler<CreateGespreksbijdrageCommand, CommandResult<Gespreksbijdrage>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;
    private readonly IBestandService _bestanden;
    private readonly IAutomatischAntwoord _automatischAntwoord;

    public CreateGespreksbijdrageCommandHandler(
        ILogger<CreateGespreksbijdrageCommandHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IGesprekEventLog eventLog,
        IBestandService bestanden,
        IAutomatischAntwoord automatischAntwoord
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _eventLog = eventLog;
        _bestanden = bestanden;
        _automatischAntwoord = automatischAntwoord;
    }

    public async Task<CommandResult<Gespreksbijdrage>> Handle(CreateGespreksbijdrageCommand request, CancellationToken cancellationToken)
    {
        var bijdrage = request.Bijdrage;

        var gesprek = await _context.LoadWithDeelnamesAsync(_rsin, bijdrage.GesprekId, cancellationToken);
        if (gesprek == null)
        {
            return Invalid("gesprek", ErrorCode.NotFound, "Het gesprek bestaat niet.");
        }

        if (gesprek.MomentEindeGesprek.HasValue)
        {
            return Invalid("gesprek", ErrorCodes.GesprekBeeindigd, "Aan een beëindigd gesprek kan geen bijdrage worden toegevoegd.");
        }

        if (
            bijdrage.IsReactieOpId.HasValue
            && !await _context.Gespreksbijdragen.AnyAsync(b => b.Id == bijdrage.IsReactieOpId && b.GesprekId == gesprek.Id, cancellationToken)
        )
        {
            return Invalid("isReactieOp", ErrorCodes.BijdrageNietInGesprek, "De bijdrage waarop wordt gereageerd hoort niet bij dit gesprek.");
        }

        var errors = await _bestanden.KoppelAsync(_rsin, bijdrage.Bijlagen, "bijlagen", cancellationToken);
        if (errors.Count > 0)
        {
            return new CommandResult<Gespreksbijdrage>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var now = DateTime.UtcNow;
        Gespreksdeelname verzender;
        var nieuweDeelname = request.NieuweVerzender != null;

        if (nieuweDeelname)
        {
            verzender = request.NieuweVerzender;
            verzender.Owner = _rsin;
            verzender.AanvangsmomentDeelname = now;
            gesprek.Deelnames.Add(verzender);
        }
        else
        {
            verzender = gesprek.Deelnames.SingleOrDefault(d => d.Id == request.VerzenderId);
            if (verzender == null)
            {
                return Invalid("verzender.gespreksdeelname", ErrorCodes.DeelnameNietInGesprek, "De gespreksdeelname hoort niet bij dit gesprek.");
            }

            if (!verzender.IsActief)
            {
                return Invalid("verzender.gespreksdeelname", ErrorCodes.DeelnameBeeindigd, "Een beëindigde deelname kan geen bijdrage leveren.");
            }
        }

        bijdrage.Owner = _rsin;
        bijdrage.Leveringsmoment = now;
        bijdrage.Verzender = verzender;

        await _context.Gespreksbijdragen.AddAsync(bijdrage, cancellationToken);

        if (nieuweDeelname)
        {
            _eventLog.Append(gesprek, EventTypes.GespreksdeelnameAangemaakt, verzender);
        }
        _eventLog.Append(gesprek, EventTypes.GespreksbijdrageAangemaakt, bijdrage);
        _automatischAntwoord.Voegtoe(gesprek, verzender, now);

        await _context.SaveChangesAsync(cancellationToken);

        return new CommandResult<Gespreksbijdrage>(bijdrage, CommandStatus.OK);
    }

    private static CommandResult<Gespreksbijdrage> Invalid(string name, string code, string reason) =>
        new(null, CommandStatus.ValidationError, new ValidationError(name, code, reason));
}

class CreateGespreksbijdrageCommand : IRequest<CommandResult<Gespreksbijdrage>>
{
    public Gespreksbijdrage Bijdrage { get; internal set; }
    public Guid? VerzenderId { get; internal set; }
    public Gespreksdeelname NieuweVerzender { get; internal set; }
}
