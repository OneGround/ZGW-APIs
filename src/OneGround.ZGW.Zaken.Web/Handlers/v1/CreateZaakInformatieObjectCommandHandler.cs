using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakInformatieObjectCommandHandler
    : ZakenBaseHandler<CreateZaakInformatieObjectCommandHandler>,
        IRequestHandler<CreateZaakInformatieObjectCommand, CommandResult<ZaakInformatieObject>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakInformatieObjectBusinessRuleService _zaakInformatieObjectBusinessRuleService;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly ICachedDocumentenServiceAgent _documentenServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakInformatieObjectCommandHandler(
        ILogger<CreateZaakInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IZaakInformatieObjectBusinessRuleService zaakInformatieObjectBusinessRuleService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        ICatalogiServiceAgent catalogiServiceAgent,
        ICachedDocumentenServiceAgent documentenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakInformatieObjectBusinessRuleService = zaakInformatieObjectBusinessRuleService;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _catalogiServiceAgent = catalogiServiceAgent;
        _documentenServiceAgent = documentenServiceAgent;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakInformatieObject>> Handle(CreateZaakInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakInformatieObject and validating....");

        var zaakInformatieObject = request.ZaakInformatieObject;

        var errors = new List<ValidationError>();

        await _zaakInformatieObjectBusinessRuleService.ValidateAsync(
            zaakInformatieObject,
            request.ZaakUrl,
            _applicationConfiguration.IgnoreInformatieObjectValidation,
            errors
        );

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            errors.Add(new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend."));
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.NotFound, errors.ToArray());
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden);
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        var informatieObject = await GetInformatieObjectAsync(zaakInformatieObject, errors);
        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.InformatieObjectTypen.Contains(informatieObject.InformatieObjectType))
        {
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.MissingZaakTypeInformatieObjectTypeRelation,
                "De referentie hoort niet bij het zaaktype van de zaak."
            );

            // TODO: Analyse issue: FUND-910 Error on ZT -IOT relation that does exist
            _logger.LogWarning("Request.informatieobject: {InformatieObject}", request.ZaakInformatieObject.InformatieObject);
            _logger.LogWarning("Request.zaak: {ZaakUrl}", request.ZaakUrl);

            _logger.LogWarning("InformatieObjectType: {InformatieObject}", informatieObject.InformatieObjectType);
            _logger.LogWarning("ZaakType: {ZaakType}", zaak.Zaaktype);
            _logger.LogWarning("ZaakType.InformatieObjectTypen: {zaakTypeInformatieObjectTypen}", string.Join(", ", zaakType.InformatieObjectTypen));
            // ----

            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            zaakInformatieObject.RegistratieDatum = DateTime.UtcNow;
            zaakInformatieObject.ZaakId = zaak.Id;
            zaakInformatieObject.Zaak = zaak;
            zaakInformatieObject.Owner = zaak.Owner;

            await _context.ZaakInformatieObjecten.AddAsync(zaakInformatieObject, cancellationToken);

            audittrail.SetNew<ZaakInformatieObjectResponseDto>(zaakInformatieObject);

            await audittrail.CreatedAsync(zaakInformatieObject.Zaak, zaakInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakInformatieObject successfully created. Id={Id}", zaakInformatieObject.Id);
        }

        var extraKenmerken = new Dictionary<string, string> { { "zaakinformatieobject.informatieobject", zaakInformatieObject.InformatieObject } };

        await SendNotificationAsync(Actie.create, zaakInformatieObject, extraKenmerken, cancellationToken);

        return new CommandResult<ZaakInformatieObject>(zaakInformatieObject, CommandStatus.OK);
    }

    private async Task<EnkelvoudigInformatieObjectResponseDto> GetInformatieObjectAsync(
        ZaakInformatieObject zaakInformatieObject,
        List<ValidationError> errors
    )
    {
        var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(zaakInformatieObject.InformatieObject);

        if (!result.Success)
        {
            errors.Add(new ValidationError("informatieobject", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private async Task<ZaakTypeResponseDto> GetZaakTypeAsync(string zaakTypeUrl, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaakTypeUrl);
        if (!result.Success)
        {
            errors.Add(new ValidationError("zaaktype", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" };
}

class CreateZaakInformatieObjectCommand : IRequest<CommandResult<ZaakInformatieObject>>
{
    public ZaakInformatieObject ZaakInformatieObject { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
