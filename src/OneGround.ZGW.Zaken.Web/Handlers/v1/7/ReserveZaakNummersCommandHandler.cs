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
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Configuration;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._7;

class ReserveZaakNummersCommandHandler : ZGWBaseHandler, IRequestHandler<ReserveZaakNummersCommand, CommandResult<IReadOnlyList<string>>>
{
    private const string NummerEntity = "zaken";
    private const string ZaaktypeTemplateKey = "{ztc}";

    private readonly ILogger<ReserveZaakNummersCommandHandler> _logger;
    private readonly ZrcDbContext _context;
    private readonly INummerGenerator _nummerGenerator;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ReserveZaakNummersCommandHandler(
        ILogger<ReserveZaakNummersCommandHandler> logger,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ZrcDbContext context,
        INummerGenerator nummerGenerator
    )
        : base(configuration, authorizationContextAccessor)
    {
        _logger = logger;
        _context = context;
        _nummerGenerator = nummerGenerator;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    public async Task<CommandResult<IReadOnlyList<string>>> Handle(ReserveZaakNummersCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Reserving {Aantal} zaaknummer(s)....", request.Aantal);

        // Note: the numbers are generated (and unique) per organisation, so they are always reserved for the organisation of the caller.
        // A different bronorganisatie is not an error, but is reported as a warning.
        var warnings = new List<string>();
        if (request.Bronorganisatie != _rsin)
        {
            warnings.Add("De bronorganisatie wijkt af van de organisatie van de aanroeper.");
        }

        var maxAantal = _applicationConfiguration.ReserveZaaknummersMaxAantal;
        if (request.Aantal > maxAantal)
        {
            return Fail(
                new ValidationError("aantal", ErrorCode.Invalid, $"Het aantal te reserveren zaaknummers mag niet groter zijn dan {maxAantal}.")
            );
        }

        // Note: a zaaktype is not known yet, so a format that needs it cannot be used (checked up front, so that no numbers are used up).
        // The format is the one of the organisation (see INummerGenerator.GetFormatAsync).
        var format = await _nummerGenerator.GetFormatAsync(_rsin, NummerEntity, cancellationToken);

        if (format == null)
        {
            return Fail(new ValidationError("nonFieldErrors", ErrorCode.Invalid, "Er is geen formaat voor zaaknummers geconfigureerd."));
        }

        if (format.Contains(ZaaktypeTemplateKey))
        {
            return Fail(
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.Invalid,
                    "Zaaknummers kunnen niet vooraf gereserveerd worden: het geconfigureerde formaat bevat het zaaktype."
                )
            );
        }

        var zaaknummers = new List<string>(request.Aantal);
        for (var i = 0; i < request.Aantal; i++)
        {
            // Note: the generated number is never handed out again (the counter only goes up), so it stays reserved for an explicit use in a new zaak
            zaaknummers.Add(await _nummerGenerator.GenerateAsync(_rsin, NummerEntity, IsZaakIdentificatieUnique, cancellationToken));
        }

        _logger.LogDebug("{Aantal} zaaknummer(s) successfully reserved.", zaaknummers.Count);

        return new CommandResult<IReadOnlyList<string>>(zaaknummers, CommandStatus.OK) { Warnings = warnings };
    }

    private bool IsZaakIdentificatieUnique(string identificatie)
    {
        return !_context.Zaken.AsNoTracking().Any(z => z.Identificatie == identificatie && z.Owner == _rsin);
    }

    private static CommandResult<IReadOnlyList<string>> Fail(ValidationError error) => new(null, CommandStatus.ValidationError, error);
}

class ReserveZaakNummersCommand : IRequest<CommandResult<IReadOnlyList<string>>>
{
    public string Bronorganisatie { get; internal set; }
    public int Aantal { get; internal set; } = 1;
}
