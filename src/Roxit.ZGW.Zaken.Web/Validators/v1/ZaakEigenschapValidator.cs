using System;
using System.Globalization;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

static class ZaakEigenschapValidator
{
    public static bool Validate(
        ZaakEigenschap zaakeigenschap,
        Catalogi.Contracts.v1.EigenschapSpecificatieDto specificatie,
        out ValidationError error
    )
    {
        error = null;

        // TODO: Bug fix: Support minimal validation (not fully implemented: kardinaliteit and waardenverzameling is missing)
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
                    if (!decimal.TryParse(zaakeigenschap.Waarde, out _))
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
