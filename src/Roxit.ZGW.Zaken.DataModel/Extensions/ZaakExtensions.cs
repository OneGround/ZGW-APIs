using System;
using System.Linq;

namespace Roxit.ZGW.Zaken.DataModel.Extensions;

public static class ZaakExtensions
{
    public const string ConversionBron = "conversie_in_progress";

    /// <summary>
    /// Indicates if zaak contains conversion kenmerk field.
    /// </summary>
    public static bool HasConversionKenmerk(this Zaak zaak)
    {
        return zaak.Kenmerken != null && zaak.Kenmerken.Any(k => IsConversion(k.Bron) && IsTrue(k.Kenmerk));
    }

    /// <summary>
    /// Indicates if zaak of the entity contains conversion kenmerk field.
    /// </summary>
    public static bool HasConversionKenmerk(this IZaakEntity zaakEntity) => zaakEntity.Zaak.HasConversionKenmerk();

    private static bool IsConversion(string bron) => string.Equals(ConversionBron, bron, StringComparison.OrdinalIgnoreCase);

    private static bool IsTrue(string val) => bool.TryParse(val, out var result) && result;
}
