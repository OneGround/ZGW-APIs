using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentValidation;
using NodaTime;
using NodaTime.Text;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Expands;

namespace Roxit.ZGW.Common.Web.Validations;

public static class IRuleBuilderExtensions
{
    /// <summary>
    /// Validates if the specified expand is enabled in the expand list
    /// </summary>
    ///
    public static IRuleBuilderOptions<T, string> IsExpandEnabled<T>(this IRuleBuilder<T, string> ruleBuilder, string allowedExpand)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                switch (allowedExpand)
                {
                    case "all":
                        return true;
                    case "none":
                        return false;
                }

                var allowedExpandsLookup = allowedExpand
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToHashSet();

                var specifiedExpands = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                return specifiedExpands.All(expand => allowedExpandsLookup.ContainsAnyOf(expand));
            })
            .WithMessage("Expand is uitgeschakeld op deze operatie.")
            .WithErrorCode(ErrorCode.DisabledExpand);
    }

    public static IRuleBuilderOptions<T, string> ExpandsValid<T>(this IRuleBuilder<T, string> ruleBuilder, IEnumerable<string> supportedExpand)
        where T : IExpandQueryParameter
    {
        var result = ruleBuilder.Custom(
            (_, b) =>
            {
                ValidateExpands(b, supportedExpand);
            }
        );

        return (IRuleBuilderOptions<T, string>)result;
    }

    private static void ValidateExpands<T>(ValidationContext<T> validatorCtx, IEnumerable<string> supportedExpand)
        where T : IExpandQueryParameter
    {
        if (validatorCtx.InstanceToValidate.Expand != null)
        {
            var supportedExpandLookup = supportedExpand.ToHashSet();

            var specifiedExpands = validatorCtx.InstanceToValidate.Expand.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var specifiedExpand in specifiedExpands)
            {
                if (!supportedExpandLookup.ContainsAnyOf(specifiedExpand))
                {
                    validatorCtx.AddFailure(
                        new FluentValidation.Results.ValidationFailure(
                            "expand",
                            $"Het ingediende veld {specifiedExpand} komt met geen enkel veld in de uitbreidbare json overeen"
                        )
                        {
                            ErrorCode = ErrorCode.InvalidExpandField,
                        }
                    );
                }
            }
        }
    }

    /// <summary>
    /// Validates if value is Rsin number.
    /// </summary>
    /// <param name="required">Indicates if value can be null.</param>
    public static IRuleBuilderOptions<T, string> IsRsin<T>(this IRuleBuilderInitial<T, string> ruleBuilderInitial, bool required)
    {
        return ruleBuilderInitial
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .When(_ => required, ApplyConditionTo.CurrentValidator)
            .Length(9)
            .WithMessage("RSIN moet 9 tekens lang zijn.")
            .WithErrorCode(ErrorCode.InvalidLength)
            .DependentRules(() =>
            {
                ruleBuilderInitial
                    .Matches("^[0-9]{9}$")
                    .WithMessage("Waarde moet numeriek zijn.")
                    .WithErrorCode(ErrorCode.OnlyDigits)
                    .DependentRules(() =>
                    {
                        ruleBuilderInitial
                            .Must(value => value == null || RsinValidator.ValidateElfProef(value))
                            .WithMessage("Onjuist RSIN nummer.")
                            .WithErrorCode(ErrorCode.Invalid);
                    });
            });
    }

    /// <summary>
    /// Validates if value is Rsin number. Used in comma separated list collection.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsRsin<T>(this IRuleBuilderInitialCollection<T, string> ruleBuilderInitial)
    {
        return ruleBuilderInitial
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Length(9)
            .WithMessage("RSIN moet 9 tekens lang zijn.")
            .WithErrorCode(ErrorCode.InvalidLength)
            .DependentRules(() =>
            {
                ruleBuilderInitial
                    .Matches("^[0-9]{9}$")
                    .WithMessage("Waarde moet numeriek zijn.")
                    .WithErrorCode(ErrorCode.OnlyDigits)
                    .DependentRules(() =>
                    {
                        ruleBuilderInitial
                            .Must(value => value == null || RsinValidator.ValidateElfProef(value))
                            .WithMessage("Onjuist RSIN nummer.")
                            .WithErrorCode(ErrorCode.Invalid);
                    });
            });
    }

    /// <summary>
    /// Validates if identificatie is in valid format.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsValidIdentificatie<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        char[] allowedChars = ['-', '_'];
        return ruleBuilder
            .MaximumLength(50)
            .WithMessage("Identificatie moet 50 tekens lang zijn.")
            .WithErrorCode(ErrorCode.InvalidLength)
            .DependentRules(() =>
            {
                ruleBuilder
                    .Must(value => string.IsNullOrWhiteSpace(value) || value.All(c => char.IsLetterOrDigit(c) || allowedChars.Contains(c)))
                    .WithMessage("Waarde moet letters, cijfers of '_' en ' - ' bevatten")
                    .WithErrorCode(ErrorCode.Invalid);
            });
    }

    /// <summary>
    /// Validates if value is a valid MIMI-type.
    /// </summary>
    /// <param name="allowEmpty">Indicates if value can be empty.</param>
    public static IRuleBuilderOptions<T, string> IsValidMimeType<T>(
        this IRuleBuilderInitial<T, string> ruleBuilderInitial,
        Func<string, bool> validate,
        int maxLength = 255,
        bool allowEmpty = false
    )
    {
        return ruleBuilderInitial
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .When(_ => !allowEmpty, ApplyConditionTo.CurrentValidator)
            .MaximumLength(maxLength)
            .WithMessage($"MIME-type mag niet langer zijn dan {maxLength} tekens.")
            .WithErrorCode(ErrorCode.MaxLength)
            .DependentRules(() =>
            {
                ruleBuilderInitial
                    .Must(value => value == "" || validate(value))
                    .WithMessage(v => $"Onjuist MIME-type '{v.GetType().GetProperty("Formaat")?.GetValue(v)}") // Note: Instance of "v" can be any of the implemented EnkelvoudigInformatieObjectCreateRequestDto classes so we use Refelction to determine the value of "Formaat"
                    .WithErrorCode(ErrorCode.Invalid);
            });
    }

    /// <summary>
    /// Validates if value is well formed uri string.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsUri<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || Uri.IsWellFormedUriString(value, UriKind.Absolute))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik een geldige uri.")
            .WithErrorCode(ErrorCode.InvalidResource);
    }

    /// <summary>
    /// Validates if value is not an uri string.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsNotUri<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || !Uri.IsWellFormedUriString(value, UriKind.Absolute))
            .WithMessage("Dit veld mag geen uri bevatten.")
            .WithErrorCode(ErrorCode.InvalidResource);
    }

    public static IRuleBuilderOptions<T, string> IsSeparatedBy<T>(this IRuleBuilder<T, string> ruleBuilder, char separator, bool mustBeFilled)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value) || value.Length == 0)
                    return false;

                var splitted = value.Split(separator);
                if (splitted.Length != 2)
                    return false;

                if (mustBeFilled && (string.IsNullOrEmpty(splitted[0]) || string.IsNullOrEmpty(splitted[1])))
                    return false;

                return true;
            })
            .WithMessage($"Veld bevat geen waarden gescheiden door het scheidingskarakter {separator}")
            .WithErrorCode(ErrorCode.InvalidResource);
    }

    /// <summary>
    /// Validates if value is ISO 8601 duration format <see href="https://www.digi.com/resources/documentation/digidocs/90001437-13/reference/r_iso_8601_duration_format.htm"/>.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsDuration<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (value == null)
                    return true;

                // Validate Period format first
                if (!PeriodPattern.NormalizingIso.Parse(value).TryGetValue(Period.Zero, out var period))
                    return false;

                // Validate on maximum period next (depending on specified format)
                if (period.Days > 50000 || period.Years > 100 || period.Months > 100 * 12)
                    return false;

                return true;
            })
            .WithMessage(
                "Tijdsduur heeft een verkeerd formaat, gebruik 1 van onderstaande formaten: P(n)Y(n)M(n)D met een maximum van 50000 dagen óf 100 jaar óf 1200 maanden."
            )
            .WithErrorCode(ErrorCode.Invalid);
    }

    /// <summary>
    /// Validate if collection contains no duplicate entries.
    /// </summary>
    public static IRuleBuilderOptions<T, IEnumerable<string>> IsDistinct<T>(this IRuleBuilder<T, IEnumerable<string>> ruleBuilder)
    {
        return ruleBuilder
            .Must(value =>
            {
                if (value == null)
                {
                    return true;
                }

                var knownElements = new HashSet<string>();

                return value.All(element => knownElements.Add(element));
            })
            .WithMessage("Verzameling bevat dubbele vermeldingen.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    public static IRuleBuilderOptions<T, string> IsDate<T>(this IRuleBuilderInitial<T, string> ruleBuilderInitial, bool required)
    {
        return ruleBuilderInitial
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .When(_ => required, ApplyConditionTo.CurrentValidator)
            .Must(value => value == null || DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik formaat: YYYY-MM-DD.")
            .WithErrorCode(ErrorCode.Invalid)
            .DependentRules(() =>
            {
                ruleBuilderInitial
                    .Must(value => value == null || DateTime.Parse(value) >= new DateTime(1753, 1, 1))
                    .WithMessage("De datum moet groter of gelijk zijn aan 1753-01-01.")
                    .WithErrorCode(ErrorCode.Invalid);
            });
    }

    public static IRuleBuilderOptions<T, string> IsDateTime<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || DateTime.TryParse(value, out _))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik ISO formaat: YYYY-MM-DDTHH:MM:SSZ.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    public static IRuleBuilderOptions<T, string> IsDateWithoutSeparator<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik formaat: YYYYMMDD.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    public static IRuleBuilderOptions<T, string> IsDateTimeWithoutSeparator<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik formaat: YYYYMMDDHHMMSS.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    public static IRuleBuilderOptions<T, string> NotInTheFuture<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || (DateTime.TryParse(value, out var parsedDate) && parsedDate < DateTime.UtcNow))
            .WithMessage("Deze waarde mag niet in de toekomst liggen.")
            .WithErrorCode(ErrorCode.FuturenotAllowed);
    }

    public static IRuleBuilderOptions<T, string> IsInteger<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || int.TryParse(value, out _))
            .WithMessage("Veld is geen nummer.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    public static IRuleBuilderOptions<T, string> IsDecimal<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Matches("^[-+]?[0-9]*\\.?[0-9]*$").WithMessage("Veld is geen decimaal getal.").WithErrorCode(ErrorCode.Invalid);
    }

    /// <summary>
    /// Validates if value is boolean.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsBoolean<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || bool.TryParse(value, out _))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik formaat: <Boolean>.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    /// <summary>
    /// Validates if value is guid.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsGuid<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || Guid.TryParse(value, out _))
            .WithMessage("Veld heeft het verkeerde formaat, gebruik formaat: <Guid>.")
            .WithErrorCode(ErrorCode.Invalid);
    }

    public static IRuleBuilderOptions<T, int> IsInRange<T>(this IRuleBuilder<T, int> ruleBuilder, int minValue, int maxValue)
    {
        return ruleBuilder
            .Must(value => value >= minValue && value <= maxValue)
            .WithMessage($"De waarde dient te liggen tussen {minValue} en {maxValue}.")
            .WithErrorCode(ErrorCode.Invalid);
    }
}
