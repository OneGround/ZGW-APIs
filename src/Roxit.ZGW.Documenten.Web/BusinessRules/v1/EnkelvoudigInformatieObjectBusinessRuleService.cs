using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Configuration;

namespace Roxit.ZGW.Documenten.Web.BusinessRules.v1;

public interface IEnkelvoudigInformatieObjectBusinessRuleService
{
    Task<bool> ValidateAsync(
        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie,
        bool ignoreInformatieObjectTypeValidation,
        Guid? existingEnkelvoudigInformatieObjectId,
        bool isPartialUpdate,
        decimal apiVersie,
        List<ValidationError> errors,
        CancellationToken cancellationToken = default
    );
}

public class EnkelvoudigInformatieObjectBusinessRuleService : IEnkelvoudigInformatieObjectBusinessRuleService
{
    private readonly ILogger<EnkelvoudigInformatieObjectBusinessRuleService> _logger;
    private readonly DrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public EnkelvoudigInformatieObjectBusinessRuleService(
        IConfiguration configuration,
        ILogger<EnkelvoudigInformatieObjectBusinessRuleService> logger,
        DrcDbContext context,
        ICatalogiServiceAgent catalogiServiceAgent
    )
    {
        _logger = logger;
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    public async Task<bool> ValidateAsync(
        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie,
        bool ignoreInformatieObjectTypeValidation,
        Guid? existingEnkelvoudigInformatieObjectId,
        bool isPartialUpdate,
        decimal apiVersie,
        List<ValidationError> errors,
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.Identificatie))
        {
            bool existingEnkelvoudigInformatieObject = await _context
                .EnkelvoudigInformatieObjectVersies.AsNoTracking()
                .AnyAsync(
                    z =>
                        z.Identificatie == enkelvoudigInformatieObjectVersie.Identificatie
                        && z.Bronorganisatie == enkelvoudigInformatieObjectVersie.Bronorganisatie
                        && z.Versie == enkelvoudigInformatieObjectVersie.Versie,
                    cancellationToken
                );

            if (existingEnkelvoudigInformatieObject)
            {
                var error = new ValidationError("identificatie", ErrorCode.Unique, "Deze identificatie bestaat al voor deze bronorganisatie.");

                errors.Add(error);
            }
        }

        bool indicatiePutGebruiksRechtenValidionError;

        if (existingEnkelvoudigInformatieObjectId.HasValue)
        {
            // Add a new version of an existing EnkelvoudigInformatieObject

            /*
            [B] UPDATE (= ADD NEW VERSION) SCENARIO - Er wordt gevalideerd op
                1. correcte lock waarde
                2. het informatieobjecttype mag niet gewijzigd worden
                3. status NIET definitief
            */

            var existing = _context
                .EnkelvoudigInformatieObjectVersies.AsNoTracking()
                .Include(e => e.EnkelvoudigInformatieObject.GebruiksRechten)
                .Where(e => e.EnkelvoudigInformatieObject.Id == existingEnkelvoudigInformatieObjectId.Value)
                .OrderBy(e => e.Versie)
                .Last();

            // Note: Locken en unlocken van documenten (drc-009)
            if (!existing.EnkelvoudigInformatieObject.Locked)
            {
                var error = new ValidationError("status", ErrorCode.InvalidForReceived, "Een unlocked document mag niet bewerkt worden.");

                errors.Add(error);

                return false;
            }
            if (string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.Lock))
            {
                ValidationError error;
                if (isPartialUpdate) // Note: To get Postman Tests get working
                {
                    error = new ValidationError("nonFieldErrors", ErrorCode.MissingLockId, "Dit is een verplicht veld.");
                }
                else
                {
                    error = new ValidationError("lock", ErrorCode.Required, "Dit is een verplicht veld.");
                }
                errors.Add(error);

                return false;
            }

            if (existing.EnkelvoudigInformatieObject.Lock != enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.Lock)
            {
                var error = new ValidationError("nonFieldErrors", ErrorCode.IncorrectLockId, "Incorrect lock ID.");

                errors.Add(error);

                return false;
            }

            // Note: To prevent creating a new version over an pending one (in case of multipart-upload) give an validation error. Multiplart: inhoud=null and Bestandsomvang is the size of complete document
            if (existing.Inhoud == null && existing.Bestandsomvang > 0)
            {
                // Pending multi-part upload on this/current version
                var error = new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.UpdateNotAllowed,
                    "Op de huidige versie is de upload van bestandsdelen actief. Zorg dat deze eerst afgerond wordt."
                );

                errors.Add(error);

                return false;
            }

            // Note: Bijwerken van documenten (drc-010)
            if (_applicationConfiguration.IgnoreBusinessRuleDrc010)
            {
                _logger.LogDebug("Warning: Business-rule DRC-010 is disabled.");
            }
            else
            {
                // De status NIET definitief is
                if (existing.Status == Status.definitief)
                {
                    var error = new ValidationError(
                        "nonFieldErrors",
                        ErrorCode.UpdateNotAllowed,
                        "Een definitief document mag niet gewijzigd worden."
                    );

                    errors.Add(error);
                }
                // Het informatieobjecttype niet gewijzigd wordt
                else if (
                    existing.EnkelvoudigInformatieObject.InformatieObjectType
                    != enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.InformatieObjectType
                )
                {
                    var error = new ValidationError("informatieobjecttype", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

                    errors.Add(error);
                }
            }

            indicatiePutGebruiksRechtenValidionError =
                existing.EnkelvoudigInformatieObject.GebruiksRechten.Count == 0
                && enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht.HasValue
                && enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht.Value;

            if (
                existing.EnkelvoudigInformatieObject.GebruiksRechten.Count != 0
                && (
                    enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht == null
                    || !enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht.Value
                )
            )
            {
                var error = new ValidationError(
                    "indicatieGebruiksrecht",
                    ErrorCode.MissingGebruiksRechten,
                    "De indicatie kan niet weggehaald worden of ongespecifieerd zijn als er Gebruiksrechten gedefinieerd zijn."
                );

                errors.Add(error);
            }
        }
        else
        {
            // Add initial (new) EnkelvoudigInformatieObject

            indicatiePutGebruiksRechtenValidionError =
                enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht.HasValue
                && enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.IndicatieGebruiksrecht.Value;

            /*
            [A] ADD SCENARIO - Er wordt gevalideerd op
                1. geldigheid informatieobjecttype URL - de resource moet opgevraagd kunnen worden uit de catalogi API en de vorm van een INFORMATIEOBJECTTYPE hebben.
                2. publicatie informatieobjecttype -concept moet false zijn
            */

            if (!ignoreInformatieObjectTypeValidation)
            {
                // Note: Valideren informatieobjecttype op de EnkelvoudigInformatieObject - resource(drc - 001)
                await ValidateInformatieObjectTypeAsync(enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObject.InformatieObjectType, errors);
            }
        }

        if (indicatiePutGebruiksRechtenValidionError)
        {
            var error = new ValidationError(
                "indicatieGebruiksrecht",
                ErrorCode.MissingGebruiksRechten,
                "De indicatie moet op 'ja' gezet worden door 'gebruiksrechten' aan te maken, dit kan niet direct op deze resource."
            );

            errors.Add(error);
        }

        // Note: Statuswijzigingen van informatieobjecten (drc-005). Updated: only older versions than 1.5!
        if (
            apiVersie < 1.5M
            && enkelvoudigInformatieObjectVersie.OntvangstDatum.HasValue
            && (
                enkelvoudigInformatieObjectVersie.Status == Status.in_bewerking || enkelvoudigInformatieObjectVersie.Status == Status.ter_vaststelling
            )
        )
        {
            var error = new ValidationError(
                "status",
                ErrorCode.InvalidForReceived,
                "De statuswaarden 'in_bewerking' en 'ter_vaststelling' zijn niet van toepassing op ontvangen documenten."
            );

            errors.Add(error);
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateInformatieObjectTypeAsync(string informatieObjectType, List<ValidationError> errors)
    {
        // Note: Valideren informatieobjecttype op de EnkelvoudigInformatieObject-resource (drc-001)

        var result = await _catalogiServiceAgent.GetInformatieObjectTypeByUrlAsync(informatieObjectType);
        if (!result.Success)
        {
            errors.Add(new ValidationError("informatieobjecttype", result.Error.Code, result.Error.Title));
        }
        else
        {
            if (result.Response.Concept)
            {
                string error = $"De URL {informatieObjectType} heeft als status Concept.";

                errors.Add(new ValidationError("informatieobjecttype", ErrorCode.NotPublished, error));
            }

            var now = DateTime.UtcNow;
            if (!DateTime.TryParse(result.Response.BeginGeldigheid, out var beginGeldigheid))
            {
                beginGeldigheid = DateTime.MinValue;
            }

            DateTime? eindeGeldigheid = null;
            if (!string.IsNullOrEmpty(result.Response.EindeGeldigheid) && DateTime.TryParse(result.Response.EindeGeldigheid, out var datetime))
            {
                eindeGeldigheid = datetime;
            }
            if (now < beginGeldigheid || (eindeGeldigheid.HasValue && now > eindeGeldigheid.Value))
            {
                string error = $"De URL {informatieObjectType} valt buiten de geldigheidsperiode.";

                errors.Add(new ValidationError("informatieobjecttype", ErrorCode.Invalid, error));
            }
        }

        return errors.Count == 0;
    }
}
