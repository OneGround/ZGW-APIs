using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1;

static class ZaakEigenschapValidator
{
    public static bool Validate(
        ZaakEigenschap zaakeigenschap,
        Catalogi.Contracts.v1.EigenschapSpecificatieDto specificatie,
        out ValidationError error
    )
    {
        error = null;

        // Note: Only the blocking checks (formaat and lengte) are done here. waardenverzameling and kardinaliteit are non-blocking: see GetWarnings
        if (specificatie != null)
        {
            switch (specificatie.Formaat)
            {
                case "tekst":
                    if (int.TryParse(specificatie.Lengte, out int maxLength)) // Note: Strange thing is that ZTC eigenschap.Specificatie.Lengte is a string
                    {
                        if (zaakeigenschap.Waarde.Length > maxLength)
                            error = new ValidationError(
                                "eigenschap",
                                ErrorCode.MaxLength,
                                $"De maximale lengte van deze eigenschap mag niet meer dan {specificatie.Lengte} tekens bevatten."
                            );
                    }
                    break;

                case "getal":
                    if (!TryParseGetal(zaakeigenschap.Waarde, out _))
                        error = new ValidationError("eigenschap", ErrorCode.Invalid, "Eigenschap-waarde is geen getal.");
                    break;

                case "datum_tijd":
                    if (!TryParseTime(zaakeigenschap.Waarde, required: true, out _))
                        error = new ValidationError("eigenschap", ErrorCode.Invalid, "Eigenschap-waarde is geen datum-tijd formaat yyyyMMddHHmmss.");
                    break;

                case "datum":
                    if (!TryParseDate(zaakeigenschap.Waarde, required: true, out _))
                        error = new ValidationError("eigenschap", ErrorCode.Invalid, "Eigenschap-waarde is geen datum formaat yyyyMMdd.");
                    break;
            }
        }

        return error == null;
    }

    /// <summary>
    /// Checks the waarde against the parts of the specificatie that are not blocking: waardenverzameling and kardinaliteit.
    /// Expects a waarde that already passed <see cref="Validate"/>.
    /// </summary>
    /// <param name="aantalOverigeWaarden">The number of other zaakeigenschappen of the same eigenschap that the zaak already has.</param>
    public static IReadOnlyList<string> GetWarnings(
        ZaakEigenschap zaakeigenschap,
        Catalogi.Contracts.v1.EigenschapSpecificatieDto specificatie,
        int aantalOverigeWaarden
    )
    {
        var warnings = new List<string>();

        if (specificatie == null)
        {
            return warnings;
        }

        if (specificatie.Waardenverzameling is { Length: > 0 } && !IsInWaardenverzameling(zaakeigenschap.Waarde, specificatie))
        {
            warnings.Add("De waarde van de zaakeigenschap komt niet voor in de waardenverzameling van de eigenschap.");
        }

        // Note: kardinaliteit is the maximum number of values per zaak: "1" up to a number, or "n" for unlimited. Anything else is not checked.
        if (
            int.TryParse(specificatie.Kardinaliteit, NumberStyles.None, CultureInfo.InvariantCulture, out var maximum)
            && aantalOverigeWaarden + 1 > maximum
        )
        {
            warnings.Add(
                $"Het aantal waarden van deze eigenschap bij de zaak overschrijdt de kardinaliteit van de eigenschap (maximaal {maximum.ToString(CultureInfo.InvariantCulture)})."
            );
        }

        return warnings;
    }

    private static bool IsInWaardenverzameling(string waarde, Catalogi.Contracts.v1.EigenschapSpecificatieDto specificatie)
    {
        if (specificatie.Formaat == "getal" && TryParseGetal(waarde, out var getal))
        {
            // Note: 1.50 and 1.5 are the same number
            return specificatie.Waardenverzameling.Any(w => TryParseGetal(w, out var candidate) && candidate == getal);
        }

        return specificatie.Waardenverzameling.Contains(waarde, StringComparer.Ordinal);
    }

    private static bool TryParseGetal(string input, out decimal output) =>
        decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out output);

    private static bool TryParseDate(string input, bool required, out DateOnly output)
    {
        output = default;

        if (required && input == null)
            return false;

        if (!DateOnly.TryParseExact(input, "yyyyMMdd", out output))
            return false;
        return true;
    }

    private static bool TryParseTime(string input, bool required, out DateTime output)
    {
        output = default;

        if (required && input == null)
            return false;

        if (!DateTime.TryParseExact(input, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out output))
            return false;
        return true;
    }
}
