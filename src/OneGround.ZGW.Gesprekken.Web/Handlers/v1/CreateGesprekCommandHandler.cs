using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Bestanden;
using OneGround.ZGW.Gesprekken.Web.BusinessRules;
using OneGround.ZGW.Gesprekken.Web.Events;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

/// <summary>
/// Creates a gesprek together with its ontvanger(s), the verzender and the first gespreksbijdrage in one transaction:
/// the information model requires at least one deelnemer and at least one bijdrage per gesprek.
/// </summary>
class CreateGesprekCommandHandler : GesprekkenBaseHandler<CreateGesprekCommandHandler>, IRequestHandler<CreateGesprekCommand, CommandResult<Gesprek>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;
    private readonly INummerGenerator _nummerGenerator;
    private readonly IGesprekBusinessRuleService _businessRules;
    private readonly IBestandService _bestanden;
    private readonly IAutomatischAntwoord _automatischAntwoord;

    public CreateGesprekCommandHandler(
        ILogger<CreateGesprekCommandHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        INummerGenerator nummerGenerator,
        IGesprekBusinessRuleService businessRules,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IGesprekEventLog eventLog,
        IBestandService bestanden,
        IAutomatischAntwoord automatischAntwoord
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _eventLog = eventLog;
        _nummerGenerator = nummerGenerator;
        _businessRules = businessRules;
        _bestanden = bestanden;
        _automatischAntwoord = automatischAntwoord;
    }

    public async Task<CommandResult<Gesprek>> Handle(CreateGesprekCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        if (request.Gesprek.AanleidingZaak != null && !await _businessRules.ValidateAanleidingZaakAsync(request.Gesprek.AanleidingZaak, errors))
        {
            return new CommandResult<Gesprek>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        errors = await _bestanden.KoppelAsync(_rsin, request.EersteBijdrage.Bijlagen, "eersteBijdrage.bijlagen", cancellationToken);
        if (errors.Count > 0)
        {
            return new CommandResult<Gesprek>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var now = DateTime.UtcNow;

        var gesprek = request.Gesprek;
        gesprek.Owner = _rsin;
        gesprek.AanvangsmomentGesprek = now;
        gesprek.Referentie = await _nummerGenerator.GenerateAsync(
            _rsin,
            "gesprekken",
            id => !_context.Gesprekken.AsNoTracking().Any(g => g.Owner == _rsin && g.Referentie == id),
            cancellationToken
        );

        foreach (var deelname in request.Ontvangers.Append(request.Verzender))
        {
            deelname.Owner = _rsin;
            deelname.AanvangsmomentDeelname = now;
            gesprek.Deelnames.Add(deelname);
        }

        var bijdrage = request.EersteBijdrage;
        bijdrage.Owner = _rsin;
        bijdrage.Leveringsmoment = now;
        bijdrage.Verzender = request.Verzender;
        gesprek.Bijdragen.Add(bijdrage);

        await _context.Gesprekken.AddAsync(gesprek, cancellationToken);

        _eventLog.Append(gesprek, EventTypes.GesprekAangemaakt, gesprek);
        foreach (var deelname in gesprek.Deelnames)
        {
            _eventLog.Append(gesprek, EventTypes.GespreksdeelnameAangemaakt, deelname);
        }
        _eventLog.Append(gesprek, EventTypes.GespreksbijdrageAangemaakt, bijdrage);
        _automatischAntwoord.Voegtoe(gesprek, request.Verzender, now);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Gesprek created. Id={Id}; Referentie={Referentie}", gesprek.Id, gesprek.Referentie);

        return new CommandResult<Gesprek>(gesprek, CommandStatus.OK);
    }
}

class CreateGesprekCommand : IRequest<CommandResult<Gesprek>>
{
    public Gesprek Gesprek { get; internal set; }
    public List<Gespreksdeelname> Ontvangers { get; internal set; } = [];
    public Gespreksdeelname Verzender { get; internal set; }
    public Gespreksbijdrage EersteBijdrage { get; internal set; }
}
