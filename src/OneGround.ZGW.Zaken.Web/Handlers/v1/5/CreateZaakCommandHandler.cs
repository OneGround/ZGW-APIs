using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Extensions;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class CreateZaakCommandHandler : ZakenBaseHandler<CreateZaakCommandHandler>, IRequestHandler<CreateZaakCommand, CommandResult<Zaak>>
{
    private readonly ZrcDbContext _context;
    private readonly INummerGenerator _nummerGenerator;
    private readonly IZaakBusinessRuleService _zaakBusinessRuleService;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakCommandHandler(
        ILogger<CreateZaakCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INummerGenerator nummerGenerator,
        IZaakBusinessRuleService zaakBusinessRuleService,
        ICatalogiServiceAgent catalogiServiceAgent,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _nummerGenerator = nummerGenerator;
        _zaakBusinessRuleService = zaakBusinessRuleService;
        _catalogiServiceAgent = catalogiServiceAgent;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<Zaak>> Handle(CreateZaakCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating Zaak and validating....");

        var zaak = request.Zaak;

        if (!_authorizationContext.IsAuthorized(request.Zaak.Zaaktype, request.Zaak.VertrouwelijkheidAanduiding, AuthorizationScopes.Zaken.Create))
        {
            return new CommandResult<Zaak>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!await _zaakBusinessRuleService.ValidateAsync(zaak, request.HoofdzaakUrl, _applicationConfiguration.IgnoreZaakTypeValidation, errors))
        {
            return new CommandResult<Zaak>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var zaaktype = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaak.Zaaktype);
        if (!zaaktype.Success)
        {
            return new CommandResult<Zaak>(
                null,
                CommandStatus.ValidationError,
                new ValidationError("zaaktype", zaaktype.Error.Code, zaaktype.Error.Title)
            );
        }

        if (string.IsNullOrEmpty(zaak.Identificatie))
        {
            var organisatie = request.Zaak.Bronorganisatie;

            _nummerGenerator.SetTemplateKeyValue("{ztc}", zaaktype.Response.Identificatie);

            var zaaknummer = await _nummerGenerator.GenerateAsync(
                organisatie,
                "zaken",
                id => IsZaakIdentificatieUnique(organisatie, id),
                cancellationToken
            );

            zaak.Identificatie = zaaknummer;
        }

        if (request.Zaak.Registratiedatum == default)
        {
            zaak.Registratiedatum = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        }

        if (zaak.VertrouwelijkheidAanduiding == VertrouwelijkheidAanduiding.nullvalue)
        {
            // If vertrouwelijkheidAanduiding in request is not specified, than fallback on the VertrouwelijkheidAanduiding specified in the zaak.zaaktype
            zaak.VertrouwelijkheidAanduiding = Enum.Parse<VertrouwelijkheidAanduiding>(zaaktype.Response.VertrouwelijkheidAanduiding);
        }

        if (!string.IsNullOrEmpty(request.HoofdzaakUrl))
        {
            var rsinFilter = GetRsinFilterPredicate<Zaak>();
            var hoofdzaak = await _context
                .Zaken.Where(rsinFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.HoofdzaakUrl), cancellationToken);

            zaak.HoofdzaakId = hoofdzaak?.Id;
        }

        var catalogusId = _uriService.GetId(zaaktype.Response.Catalogus);

        zaak.Owner = _rsin;
        zaak.CatalogusId = catalogusId;
        if (zaak.Verlenging != null)
            zaak.Verlenging.Owner = _rsin;
        zaak.Kenmerken.ForEach(k => k.Owner = _rsin);
        zaak.RelevanteAndereZaken.ForEach(r => r.Owner = _rsin);

        await _context.Zaken.AddAsync(zaak, cancellationToken); // Note: Sequential Guid for Id is generated here by EF

        Geometry savedZaakgeometrie = null;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetNew<ZaakResponseDto>(zaak);

            await audittrail.CreatedAsync(zaak, zaak, cancellationToken);

            // Note: We always store zaakgeometrie on RDS (28992). So we convert if SRID differs
            if (zaak.Zaakgeometrie != null && request.SRID.Value != 28992) // If not set in RDS then we have to convert to RDS
            {
                var conversionResult = await _context.TryConvertZaakGeometrieAsync(zaak.Zaakgeometrie, cancellationToken);
                if (conversionResult.geometrie == null)
                {
                    var error = new ValidationError("zaakgeometrie", ErrorCode.Invalid, conversionResult.error);

                    return new CommandResult<Zaak>(null, CommandStatus.ValidationError, [error]);
                }

                // But we want to return the one we specified so save the original one to restore later
                savedZaakgeometrie = zaak.Zaakgeometrie;

                zaak.Zaakgeometrie = conversionResult.geometrie;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Zaak successfully created. Id={Id}; Identification={Identificatie}", zaak.Id, zaak.Identificatie);

        await SendNotificationAsync(Actie.create, zaak, cancellationToken);

        // Restore the original Zaakgeometrie
        if (savedZaakgeometrie != null)
        {
            zaak.Zaakgeometrie = savedZaakgeometrie;
        }

        return new CommandResult<Zaak>(zaak, CommandStatus.OK);
    }

    private bool IsZaakIdentificatieUnique(string organisatie, string identificatie)
    {
        return !_context.Zaken.AsNoTracking().Any(z => z.Identificatie == identificatie && z.Bronorganisatie == organisatie);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" };
}

class CreateZaakCommand : IRequest<CommandResult<Zaak>>
{
    public Zaak Zaak { get; internal set; }
    public string HoofdzaakUrl { get; internal set; }
    public int? SRID { get; internal set; }
}
