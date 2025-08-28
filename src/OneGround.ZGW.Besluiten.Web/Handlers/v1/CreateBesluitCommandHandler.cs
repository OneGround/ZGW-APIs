using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Besluiten.Web.BusinessRules;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.ServiceAgent.v1;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class CreateBesluitCommandHandler : BesluitenBaseHandler<CreateBesluitCommandHandler>, IRequestHandler<CreateBesluitCommand, CommandResult<Besluit>>
{
    private readonly BrcDbContext _context;
    private readonly INummerGenerator _nummerGenerator;
    private readonly IBesluitBusinessRuleService _besluitBusinessRuleService;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;

    public CreateBesluitCommandHandler(
        ILogger<CreateBesluitCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        IEntityUriService uriService,
        INummerGenerator nummerGenerator,
        IBesluitBusinessRuleService besluitBusinessRuleService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IZakenServiceAgent zakenServiceAgent,
        ICatalogiServiceAgent catalogiServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, besluitKenmerkenResolver)
    {
        _context = context;
        _nummerGenerator = nummerGenerator;
        _besluitBusinessRuleService = besluitBusinessRuleService;
        _auditTrailFactory = auditTrailFactory;
        _zakenServiceAgent = zakenServiceAgent;
        _catalogiServiceAgent = catalogiServiceAgent;
    }

    public async Task<CommandResult<Besluit>> Handle(CreateBesluitCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating Besluit and validating....");

        var besluit = request.Besluit;

        if (!_authorizationContext.IsAuthorized(besluit))
        {
            return new CommandResult<Besluit>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (
            !await _besluitBusinessRuleService.ValidateAsync(
                besluit,
                _applicationConfiguration.IgnoreBesluitTypeValidation,
                _applicationConfiguration.IgnoreZaakValidation,
                errors
            )
        )
        {
            return new CommandResult<Besluit>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var besluittype = await _catalogiServiceAgent.GetBesluitTypeByUrlAsync(besluit.BesluitType);
        if (!besluittype.Success)
        {
            return new CommandResult<Besluit>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("besluittype", besluittype.Error.Code, besluittype.Error.Title)
            );
        }
        var catalogusId = _uriService.GetId(besluittype.Response.Catalogus);

        if (string.IsNullOrEmpty(besluit.Identificatie))
        {
            var organisatie = request.Besluit.VerantwoordelijkeOrganisatie;

            var besluitnummer = await _nummerGenerator.GenerateAsync(
                organisatie,
                "besluiten",
                id => IsBesluitIdentificatieUnique(organisatie, id),
                cancellationToken
            );

            besluit.Identificatie = besluitnummer;
        }

        await _context.Besluiten.AddAsync(besluit, cancellationToken); // Note: Sequential Guid for Id is generated here by EF

        besluit.Owner = _rsin;
        besluit.CatalogusId = catalogusId;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<BesluitResponseDto>(besluit);

            await audittrail.CreatedAsync(besluit, besluit, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Besluit successfully created. Url={Id}; Identification={Identificatie}", besluit.Id, besluit.Identificatie);

        if (besluit.Zaak != null)
        {
            var zaakBesluit = await _zakenServiceAgent.AddZaakBesluitByUrlAsync(_uriService.GetId(besluit.Zaak), _uriService.GetUri(besluit));

            if (!zaakBesluit.Success)
            {
                _logger.LogError(
                    "Could not add zaak-besluit to ZRC. Zaak url={zaak}; Besluit={besluit}. Status={status}. Error={detail}.",
                    besluit.Zaak,
                    besluit.Url,
                    zaakBesluit.Error.Status,
                    zaakBesluit.Error.Detail
                );
                // Note: Because we have saved the zaak already the notification will be sent
            }
            else
            {
                besluit.ZaakBesluitUrl = zaakBesluit.Response.Url;

                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        await SendNotificationAsync(Actie.create, besluit, cancellationToken);

        return new CommandResult<Besluit>(besluit, CommandStatus.OK);
    }

    private bool IsBesluitIdentificatieUnique(string organisatie, string identificatie)
    {
        return !_context.Besluiten.AsNoTracking().Any(b => b.Identificatie == identificatie && organisatie == b.VerantwoordelijkeOrganisatie);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" };
}

class CreateBesluitCommand : IRequest<CommandResult<Besluit>>
{
    public Besluit Besluit { get; internal set; }
}
