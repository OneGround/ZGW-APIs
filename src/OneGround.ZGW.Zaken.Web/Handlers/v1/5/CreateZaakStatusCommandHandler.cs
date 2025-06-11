using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime.Text;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class CreateZaakStatusCommandHandler
    : ZakenBaseHandler<CreateZaakStatusCommandHandler>,
        IRequestHandler<CreateZaakStatusCommand, CommandResult<ZaakStatus>>
{
    private readonly ZrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IDocumentenServiceAgent _documentenServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IBronDateServiceFactory _bronDateServiceFactory;
    private readonly IZaakBusinessRuleService _zaakBusinessRuleService;

    public CreateZaakStatusCommandHandler(
        ILogger<CreateZaakStatusCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        ICatalogiServiceAgent catalogiServiceAgent,
        INotificatieService notificatieService,
        IDocumentenServiceAgent documentenServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        IBronDateServiceFactory bronDateServiceFactory,
        IZaakBusinessRuleService zaakBusinessRuleService,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _documentenServiceAgent = documentenServiceAgent;
        _auditTrailFactory = auditTrailFactory;
        _bronDateServiceFactory = bronDateServiceFactory;
        _zaakBusinessRuleService = zaakBusinessRuleService;
    }

    public async Task<CommandResult<ZaakStatus>> Handle(CreateZaakStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakStatus....");

        var zaakStatus = request.ZaakStatus;

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.AsSplitQuery()
            .Where(rsinFilter)
            .Include(z => z.ZaakInformatieObjecten)
            .Include(z => z.Kenmerken)
            .Include(z => z.Resultaat)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakStatus>(null, CommandStatus.ValidationError, error);
        }

        if (!_authorizationContext.IsAuthorized(zaak, AuthorizationScopes.Zaken.Statuses.Add, AuthorizationScopes.Zaken.Reopen))
        {
            var zaakStatusExists = await _context.ZaakStatussen.AsNoTracking().AnyAsync(z => z.ZaakId == zaak.Id, cancellationToken);

            if (zaakStatusExists)
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.PermissionDenied,
                    $"Met de '{AuthorizationScopes.Zaken.Create}' scope mag je slechts 1 status zetten"
                );
                return new CommandResult<ZaakStatus>(null, CommandStatus.Forbidden, error);
            }
        }

        var errors = new List<ValidationError>();

        var oldstatussen = _context.ZaakStatussen.Where(s => s.ZaakId == zaak.Id);
        if (oldstatussen.Any(s => s.DatumStatusGezet == request.ZaakStatus.DatumStatusGezet))
        {
            errors.Add(new ValidationError("nonFieldErrors", ErrorCode.Unique, "De velden zaak, datumStatusGezet moeten een unieke set zijn."));
        }

        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakStatus>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.StatusTypen.Contains(zaakStatus.StatusType))
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.ZaakTypeMismatch, "De referentie hoort niet bij het zaaktype van de zaak.");

            return new CommandResult<ZaakStatus>(null, CommandStatus.ValidationError, error);
        }

        var statusType = await GetStatusTypeAsync(zaakStatus, errors);
        if (statusType.IsEindStatus) //About to close zaak.
        {
            if (zaak.Resultaat == null)
            {
                var error = new ValidationError(
                    "zaak.resultaat",
                    ErrorCode.Invalid,
                    $"Zaak {zaak.Id} heeft geen resultaat en kan niet worden gesloten."
                );

                return new CommandResult<ZaakStatus>(null, CommandStatus.ValidationError, error);
            }

            var resultaatType = await GetResultaatTypeAsync(zaak.Resultaat, errors);
            await IsIndicatieGebruiksrechtSetAsync(zaak, errors);

            // zrc-022: Archiefstatus kan alleen een waarde anders dan "nog_te_archiveren" hebben indien
            // van alle gerelateeerde INFORMATIEOBJECTen het attribuut status de waarde "gearchiveerd" heeft.
            await _zaakBusinessRuleService.ValidateZaakDocumentenArchivedStatusAsync(zaak, errors);

            zaak.Einddatum = statusType.IsEindStatus ? DateOnly.FromDateTime(zaakStatus.DatumStatusGezet) : default(DateOnly?);

            if (statusType.IsEindStatus)
            {
                zaak.Archiefstatus = ArchiefStatus.gearchiveerd;

                if (!zaak.Archiefnominatie.HasValue)
                {
                    if (Enum.TryParse<ArchiefNominatie>(resultaatType.ArchiefNominatie, out var archiefNominatie))
                    {
                        zaak.Archiefnominatie = archiefNominatie;
                    }
                }
                if (!zaak.Archiefactiedatum.HasValue)
                {
                    if (PeriodPattern.NormalizingIso.Parse(resultaatType.ArchiefActieTermijn).TryGetValue(null, out var archiefActieTermijn))
                    {
                        var errorsArchiefactiedatum = new List<ArchiveValidationError>();

                        var bronDateService = _bronDateServiceFactory.Create(resultaatType, errorsArchiefactiedatum);
                        var bronDate = await bronDateService.GetAsync(zaak, errorsArchiefactiedatum, cancellationToken);

                        if (errorsArchiefactiedatum.Count != 0)
                        {
                            _logger.LogInformation("Archiefactiedatum fouten:");
                            // Note: log only not a real error for now
                            foreach (var error in errorsArchiefactiedatum)
                            {
                                _logger.LogInformation("Archiefactiedatum-fout: {Name}-{Code}: {Reason}", error.Name, error.Code, error.Reason);
                            }
                        }
                        else if (bronDate.HasValue)
                        {
                            // Rule for ZRC-026 calculation of startdatumbewaartermijn
                            if (zaak.Archiefnominatie == ArchiefNominatie.vernietigen)
                            {
                                zaak.StartdatumBewaartermijn = bronDate;
                            }

                            // Rule for ZRC-021 calculation of archiefactiedatum (date of destroy)
                            bronDate = bronDate.Value.AddMonths(archiefActieTermijn.Months);
                            bronDate = bronDate.Value.AddYears(archiefActieTermijn.Years);
                            bronDate = bronDate.Value.AddDays(archiefActieTermijn.Days);

                            zaak.Archiefactiedatum = bronDate;
                        }
                    }

                    // If Archiefactiedatum has a not-calculated value set zaak.Archiefstatus to gearchiveerd_procestermijn_onbekend
                    if (!zaak.Archiefactiedatum.HasValue)
                    {
                        zaak.Archiefstatus = ArchiefStatus.gearchiveerd_procestermijn_onbekend;
                    }
                }
            }
        }
        else if (zaak.Einddatum.HasValue) //About to reopen zaak
        {
            if (!_authorizationContext.IsAuthorized(zaak, AuthorizationScopes.Zaken.Reopen))
            {
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.PermissionDenied,
                    "Reopening a closed case with current scope is forbidden"
                );
                return new CommandResult<ZaakStatus>(null, CommandStatus.Forbidden, error);
            }

            zaak.Archiefnominatie = null;
            zaak.Archiefactiedatum = null;
            zaak.Einddatum = null;
            zaak.Archiefstatus = ArchiefStatus.nog_te_archiveren;
        }

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakStatus>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            // Reset (all) previous zaakstatus IndicatieLaatstGezetteStatus
            oldstatussen.ToList().ForEach(s => s.IndicatieLaatstGezetteStatus = false);

            // Create new zaakstatus and set IndicatieLaatstGezetteStatus to true
            await _context.ZaakStatussen.AddAsync(zaakStatus, cancellationToken);

            zaakStatus.ZaakId = zaak.Id;
            zaakStatus.Zaak = zaak;
            zaakStatus.Owner = zaak.Owner;
            zaakStatus.IndicatieLaatstGezetteStatus = true;

            audittrail.SetNew<ZaakStatusCreateResponseDto>(zaakStatus);

            await audittrail.CreatedAsync(zaakStatus.Zaak, zaakStatus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakStatus {zaakStatusId} successfully created.", zaakStatus.Id);

        await SendNotificationAsync(Actie.create, zaakStatus, cancellationToken);

        return new CommandResult<ZaakStatus>(zaakStatus, CommandStatus.OK);
    }

    private async Task IsIndicatieGebruiksrechtSetAsync(Zaak zaak, List<ValidationError> errors)
    {
        var zaakInformatieObjecten = await _context.ZaakInformatieObjecten.AsNoTracking().Where(z => z.ZaakId == zaak.Id).ToListAsync();

        var informatieObjectUrls = zaakInformatieObjecten.Select(i => i.InformatieObject);

        foreach (var url in informatieObjectUrls)
        {
            var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(url);
            if (!result.Success)
            {
                errors.Add(new ValidationError("informatieobject", result.Error.Code, result.Error.Title));
            }

            if (result.Success)
            {
                if (!result.Response.IndicatieGebruiksrecht.HasValue)
                {
                    errors.Add(
                        new ValidationError("nonFieldErrors", ErrorCode.IndicatieGebruiksrechtUnset, "IndicatieGebruiksrecht does not have a value.")
                    );
                }
            }
        }
    }

    private async Task<StatusTypeResponseDto> GetStatusTypeAsync(ZaakStatus zaakStatus, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetStatusTypeByUrlAsync(zaakStatus.StatusType);

        if (!result.Success)
        {
            errors.Add(new ValidationError("statustype", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private async Task<ResultaatTypeResponseDto> GetResultaatTypeAsync(ZaakResultaat zaakResultaat, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetResultaatTypeByUrlAsync(zaakResultaat.ResultaatType);
        if (!result.Success)
        {
            errors.Add(new ValidationError("resultaattype", result.Error.Code, result.Error.Title));
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

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "status" };
}

class CreateZaakStatusCommand : IRequest<CommandResult<ZaakStatus>>
{
    public string ZaakUrl { get; internal set; }
    public ZaakStatus ZaakStatus { get; internal set; }
}
