using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.Contracts.v1.Responses;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Bestanden;
using OneGround.ZGW.Gesprekken.Web.Events;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

/// <summary>
/// Redacts a gespreksbijdrage that should not have been there (beheer): the bijdrage, its bijlagen with their
/// bestanden and its leesregistraties are removed, and so is their content in the event history. The events keep their sequence, so a
/// cursor stays valid; the stream skips them. Consumers get gespreksbijdrage.geredigeerd with the sequences whose
/// content is gone, and remove that content from their own storage too.
/// </summary>
class RedigeerGespreksbijdrageCommandHandler
    : GesprekkenBaseHandler<RedigeerGespreksbijdrageCommandHandler>,
        IRequestHandler<RedigeerGespreksbijdrageCommand, CommandResult<RedactieResponseDto>>
{
    private readonly GrcDbContext _context;
    private readonly IGesprekEventLog _eventLog;
    private readonly IBestandOpslag _opslag;

    public RedigeerGespreksbijdrageCommandHandler(
        ILogger<RedigeerGespreksbijdrageCommandHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IGesprekEventLog eventLog,
        IBestandOpslag opslag
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _eventLog = eventLog;
        _opslag = opslag;
    }

    public async Task<CommandResult<RedactieResponseDto>> Handle(RedigeerGespreksbijdrageCommand request, CancellationToken cancellationToken)
    {
        var bijdrage = await _context
            .Gespreksbijdragen.Include(b => b.Bijlagen)
            .Where(b => b.Owner == _rsin)
            .SingleOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (bijdrage == null)
        {
            return new CommandResult<RedactieResponseDto>(null, CommandStatus.NotFound);
        }

        var gesprek = await _context.LoadWithDeelnamesAsync(_rsin, bijdrage.GesprekId, cancellationToken);
        var url = _uriService.GetUri(bijdrage);

        var geredigeerd = await FindEventsAboutAsync(bijdrage, url, cancellationToken);
        foreach (var record in geredigeerd)
        {
            record.Geredigeerd = true;
            record.Data = "{}";
        }

        var gelezen = await _context.GelezenGespreksbijdragen.Where(g => g.GespreksbijdrageId == bijdrage.Id).ToListAsync(cancellationToken);
        _context.GelezenGespreksbijdragen.RemoveRange(gelezen);
        var bestandIds = bijdrage.Bijlagen.Select(b => b.BestandId).ToList();
        var bestanden = await _context.Bestanden.Where(b => bestandIds.Contains(b.Id)).ToListAsync(cancellationToken);
        _context.Bestanden.RemoveRange(bestanden);
        _context.Bijlagen.RemoveRange(bijdrage.Bijlagen);
        _context.Gespreksbijdragen.Remove(bijdrage);

        var redactie = new RedactieResponseDto { Url = url, GeredigeerdeSequences = geredigeerd.Select(e => e.Sequence.ToString()).ToList() };
        _eventLog.Append(gesprek, EventTypes.GespreksbijdrageGeredigeerd, redactie);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var bestand in bestanden)
        {
            await _opslag.VerwijderAsync(bestand.Sleutel, cancellationToken);
        }

        // The reason belongs in an audit trail; this component has none yet, so it goes to the log
        _logger.LogInformation("Gespreksbijdrage {Url} geredigeerd. Reden: {Reden}", url, request.Reden);

        return new CommandResult<RedactieResponseDto>(redactie, CommandStatus.OK);
    }

    /// <summary>The events whose data is this bijdrage or a leesregistratie of it.</summary>
    private async Task<List<GesprekEventRecord>> FindEventsAboutAsync(Gespreksbijdrage bijdrage, string url, CancellationToken cancellationToken)
    {
        var candidates = await _context
            .Events.Where(e =>
                e.Owner == _rsin
                && e.GesprekId == bijdrage.GesprekId
                && !e.Geredigeerd
                && (e.Type == EventTypes.GespreksbijdrageAangemaakt || e.Type == EventTypes.GespreksbijdrageGelezen)
            )
            .OrderBy(e => e.Sequence)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(e =>
            {
                var data = JObject.Parse(e.Data);
                return e.Type == EventTypes.GespreksbijdrageAangemaakt
                    ? data.Value<string>("uuid") == bijdrage.Id.ToString()
                    : data.Value<string>("gespreksbijdrage") == url;
            })
            .ToList();
    }
}

class RedigeerGespreksbijdrageCommand : IRequest<CommandResult<RedactieResponseDto>>
{
    public Guid Id { get; set; }
    public string Reden { get; set; }
}
