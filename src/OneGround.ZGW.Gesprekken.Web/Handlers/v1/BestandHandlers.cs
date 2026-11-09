using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Bestanden;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

/// <summary>
/// Starts an upload (patterns/file-upload): registers the bestand and returns a temporary URL to send it to.
/// </summary>
class CreateUploadSessieCommandHandler
    : GesprekkenBaseHandler<CreateUploadSessieCommandHandler>,
        IRequestHandler<CreateUploadSessieCommand, CommandResult<UploadSessie>>
{
    private static readonly TimeSpan Geldigheid = TimeSpan.FromMinutes(15);

    private readonly GrcDbContext _context;
    private readonly IBestandOpslag _opslag;

    public CreateUploadSessieCommandHandler(
        ILogger<CreateUploadSessieCommandHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBestandOpslag opslag
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _opslag = opslag;
    }

    public async Task<CommandResult<UploadSessie>> Handle(CreateUploadSessieCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var bestand = request.Bestand;
        bestand.Id = Guid.NewGuid();
        bestand.Owner = _rsin;
        bestand.Status = BestandStatus.uploadInAfwachting;
        bestand.AangemaaktOp = now;
        bestand.VerlooptOp = now.Add(Geldigheid);
        bestand.Sleutel = $"{_rsin}/{bestand.Id}";

        await _context.Bestanden.AddAsync(bestand, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var uploadUrl = await _opslag.GetUploadUrlAsync(bestand.Sleutel, bestand.ContentType, bestand.VerlooptOp);

        return new CommandResult<UploadSessie>(new UploadSessie(bestand, uploadUrl), CommandStatus.OK);
    }
}

class CreateUploadSessieCommand : IRequest<CommandResult<UploadSessie>>
{
    public Bestand Bestand { get; set; }
}

record UploadSessie(Bestand Bestand, string UploadUrl);

class GetBestandQueryHandler : GesprekkenBaseHandler<GetBestandQueryHandler>, IRequestHandler<GetBestandQuery, QueryResult<BestandMetDownload>>
{
    private static readonly TimeSpan Geldigheid = TimeSpan.FromMinutes(15);

    private readonly GrcDbContext _context;
    private readonly IBestandService _bestanden;
    private readonly IBestandOpslag _opslag;

    public GetBestandQueryHandler(
        ILogger<GetBestandQueryHandler> logger,
        IConfiguration configuration,
        GrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBestandService bestanden,
        IBestandOpslag opslag
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _bestanden = bestanden;
        _opslag = opslag;
    }

    public async Task<QueryResult<BestandMetDownload>> Handle(GetBestandQuery request, CancellationToken cancellationToken)
    {
        var bestand = await _context.Bestanden.Where(b => b.Owner == _rsin).SingleOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (bestand == null)
        {
            return new QueryResult<BestandMetDownload>(null, QueryStatus.NotFound);
        }

        await _bestanden.VernieuwStatusAsync(bestand, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var downloadUrl =
            bestand.Status == BestandStatus.beschikbaar
                ? await _opslag.GetDownloadUrlAsync(bestand.Sleutel, bestand.Bestandsnaam, DateTime.UtcNow.Add(Geldigheid))
                : null;

        return new QueryResult<BestandMetDownload>(new BestandMetDownload(bestand, downloadUrl), QueryStatus.OK);
    }
}

class GetBestandQuery : IRequest<QueryResult<BestandMetDownload>>
{
    public Guid Id { get; set; }
}

record BestandMetDownload(Bestand Bestand, string DownloadUrl);
