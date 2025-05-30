using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Web.Configuration;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Helpers;

namespace Roxit.ZGW.Catalogi.Web.BusinessRules;

public class ConceptBusinessRule : IConceptBusinessRule
{
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ILogger<ConceptBusinessRule> _logger;

    public ConceptBusinessRule(IConfiguration configuration, ILogger<ConceptBusinessRule> logger)
    {
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        _logger = logger;
    }

    public bool ValidateConcept(IConceptEntity entity, List<ValidationError> errors)
    {
        return Validate(entity, errors, ErrorCode.NonConceptObject);
    }

    public bool ValidateConceptRelation(IConceptEntity entity, List<ValidationError> errors, decimal version)
    {
        if (version < 1.3M && _applicationConfiguration.IgnoreBusinessRulesZtc010AndZtc011)
        {
            _logger.LogDebug("Warning: Business-rules ZTC-010/ZTC-011 are disabled.");

            return true;
        }

        return Validate(entity, errors, ErrorCode.NonConceptRelation);
    }

    public bool ValidateConceptZaakType(IConceptEntity entity, List<ValidationError> errors)
    {
        return Validate(entity, errors, ErrorCode.NonConceptZaakType);
    }

    public bool ValidateNonConceptRelation(IConceptEntity entity, List<ValidationError> errors)
    {
        if (entity.Concept)
        {
            errors.Add(new ValidationError("nonFieldErrors", ErrorCode.ConceptRelation, "All related resources should be published"));
        }

        return errors.Count == 0;
    }

    private static bool Validate(IConceptEntity entity, List<ValidationError> errors, string errorCode)
    {
        if (!entity.Concept)
        {
            errors.Add(new ValidationError("nonFieldErrors", errorCode, "Het is niet toegestaan om een non-concept object bij te werken."));
        }

        return errors.Count == 0;
    }

    public bool ValidateGeldigheid(List<IConceptEntity> entities, IConceptEntity entity, List<ValidationError> errors)
    {
        if (entity.Concept)
        {
            return true;
        }

        if (
            entities.Any(t =>
                !t.Concept
                && DateTimeHelpers.IsOverlapped(
                    beginDate1: entity.BeginGeldigheid,
                    endDate1: entity.EindeGeldigheid.GetValueOrDefault(DateOnly.MaxValue),
                    beginDate2: t.BeginGeldigheid,
                    endDate2: t.EindeGeldigheid.GetValueOrDefault(DateOnly.MaxValue)
                )
            )
        )
        {
            errors.Add(
                new ValidationError(
                    "nonFieldErrors",
                    ErrorCode.Invalid,
                    "De identificatie van de entiteit is al gebruikt binnen de geldigheidsperiode."
                )
            );
        }

        return errors.Count == 0;
    }
}
