using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Extensions;

namespace OneGround.ZGW.Autorisaties.Common.BusinessRules;

public class ApplicatieBusinessRuleService : IApplicatieBusinessRuleService
{
    private readonly AcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IConfiguration _configuration;

    public ApplicatieBusinessRuleService(AcDbContext context, ICatalogiServiceAgent catalogiServiceAgent, IConfiguration configuration)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _configuration = configuration;
    }

    public async Task<bool> ValidateAddAsync(Applicatie applicatie, List<ValidationError> errors, bool checkComponentUrl = true)
    {
        ValidateAuthorizationSpecification(applicatie, errors);
        await ValidateClientIdsUniquenessAsync(applicatie, errors);

        foreach (var (autorisatie, index) in applicatie.Autorisaties.WithIndex())
        {
            await ValidateAutorisatie(autorisatie, index, errors, checkComponentUrl);
        }

        return errors.Count == 0;
    }

    public async Task<bool> ValidateUpdateAsync(
        Applicatie existingApp,
        Applicatie newApp,
        List<ValidationError> errors,
        bool checkComponentUrl = true
    )
    {
        ValidateAuthorizationSpecification(newApp, errors);
        await ValidateClientIdsUniquenessAsync(newApp, errors, existingApp.Id);

        foreach (var (autorisatie, index) in newApp.Autorisaties.WithIndex())
        {
            await ValidateAutorisatie(autorisatie, index, errors, checkComponentUrl);
        }

        return errors.Count == 0;
    }

    //ac-001
    private async Task ValidateClientIdsUniquenessAsync(Applicatie applicatie, List<ValidationError> errors, Guid existingId = default)
    {
        var uniqueClientIds = applicatie.ClientIds.Select(c => c.ClientId.ToLower()).Distinct().ToList();
        var duplicateClientIdApplicatie = await _context.Applicaties.FirstOrDefaultAsync(a =>
            a.ClientIds.Any(id => uniqueClientIds.Contains(id.ClientId.ToLower()))
        );

        if (duplicateClientIdApplicatie != null && duplicateClientIdApplicatie.Id != existingId)
        {
            errors.Add(
                new ValidationError(
                    "clientIds",
                    ErrorCode.ClientIdExists,
                    $"The clientID(s) {string.Join(", ", applicatie.ClientIds.Select(c => c.ClientId))} are already used in application {duplicateClientIdApplicatie.Id}"
                )
            );
        }
    }

    //ac-002
    private static void ValidateAuthorizationSpecification(Applicatie applicatie, List<ValidationError> errors)
    {
        if (applicatie.HeeftAlleAutorisaties && applicatie.Autorisaties.Count != 0)
            errors.Add(
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.AmbiguousAuthorizationsSpecified,
                    "Either autorisaties or heeft_alle_autorisaties should be specified"
                )
            );

        if (!applicatie.HeeftAlleAutorisaties && applicatie.Autorisaties.Count == 0)
            errors.Add(
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.MissingAuthorizations,
                    "Either autorisaties or heeft_alle_autorisaties should be specified"
                )
            );
    }

    //ac-003
    private async Task ValidateAutorisatie(Autorisatie applicatieAutorisatie, int index, List<ValidationError> errors, bool checkComponentUrl = true)
    {
        switch (applicatieAutorisatie.Component)
        {
            case Component.drc:
                ValidateDrcComponentRules(applicatieAutorisatie, index, errors, checkComponentUrl);
                break;
            case Component.brc:
                ValidateBrcComponentRules(applicatieAutorisatie, index, errors, checkComponentUrl);
                break;
            case Component.zrc:
                await ValidateZrcComponentRules(applicatieAutorisatie, index, errors, checkComponentUrl);
                break;
            default:
                return;
        }
    }

    //ac-003-1
    private async Task ValidateZrcComponentRules(
        Autorisatie applicatieAutorisatie,
        int index,
        List<ValidationError> validationErrors,
        bool checkComponentUrl = true
    )
    {
        if (applicatieAutorisatie.Scopes.Any(s => s.StartsWith("zaken.")))
        {
            MaxVertrouwelijkheidaanduidingValidator(applicatieAutorisatie, index, validationErrors);
            if (!checkComponentUrl)
            {
                return;
            }
            if (string.IsNullOrEmpty(applicatieAutorisatie.ZaakType))
            {
                validationErrors.Add(
                    new ValidationError(
                        $"autorisaties.{index}.zaaktype",
                        ErrorCode.Required,
                        $"This field is required if `component` is {applicatieAutorisatie.Component}"
                    )
                );
                return;
            }

            if (!_configuration.GetValue("Application:IgnoreZaakTypeValidation", false))
            {
                await ValidateZaakTypeExistence(applicatieAutorisatie.ZaakType, index, validationErrors);
            }
        }
    }

    private async Task ValidateZaakTypeExistence(string zaakType, int index, List<ValidationError> validationErrors)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaakType);
        if (!result.Success)
        {
            validationErrors.Add(new ValidationError($"autorisaties.{index}.zaakType", result.Error.Code, result.Error.Title));
        }
    }

    //ac-003-2
    private static void ValidateDrcComponentRules(
        Autorisatie applicatieAutorisatie,
        int index,
        List<ValidationError> validationErrors,
        bool checkComponentUrl = true
    )
    {
        if (applicatieAutorisatie.Scopes.Any(s => s.StartsWith("documenten.")))
        {
            MaxVertrouwelijkheidaanduidingValidator(applicatieAutorisatie, index, validationErrors);

            if (checkComponentUrl && string.IsNullOrEmpty(applicatieAutorisatie.InformatieObjectType))
            {
                validationErrors.Add(
                    new ValidationError(
                        $"autorisaties.{index}.informatieobjecttype",
                        ErrorCode.Required,
                        "This field is required if `component` is drc"
                    )
                );
            }
        }
    }

    //ac-003-3
    private static void ValidateBrcComponentRules(
        Autorisatie applicatieAutorisatie,
        int index,
        List<ValidationError> validationErrors,
        bool checkComponentUrl = true
    )
    {
        if (applicatieAutorisatie.Scopes.Any(s => s.StartsWith("besluiten.")) && checkComponentUrl && string.IsNullOrEmpty(applicatieAutorisatie.BesluitType))
        {
            validationErrors.Add(
                new ValidationError(
                    $"autorisaties.{index}.besluittype",
                    ErrorCode.Required,
                    $"This field is required if `component` is {applicatieAutorisatie.Component}"
                )
            );
        }
    }

    private static void MaxVertrouwelijkheidaanduidingValidator(Autorisatie applicatieAutorisatie, int index, List<ValidationError> validationErrors)
    {
        if (!applicatieAutorisatie.MaxVertrouwelijkheidaanduiding.HasValue)
        {
            validationErrors.Add(
                new ValidationError(
                    $"autorisaties.{index}.maxVertrouwelijkheidaanduiding",
                    ErrorCode.Required,
                    $"This field is required if `component` is {applicatieAutorisatie.Component}"
                )
            );
        }
    }
}
