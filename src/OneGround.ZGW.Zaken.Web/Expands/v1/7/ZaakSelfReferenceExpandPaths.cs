using System;
using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Builds the nested <see cref="IExpandResolver{TEntity}.AdditionalPaths"/> tuples shared by every
/// self-referential ZAAK expand path (currently "hoofdzaak" and "deelzaken") -- both resolve the exact
/// same nested ZAAK-expand graph, so this is the single source of truth that keeps them from silently
/// drifting apart as new nested expands are added.
/// </summary>
internal static class ZaakSelfReferenceExpandPaths
{
    public static IEnumerable<(string Path, string Parent)> Build(string rootPath) =>
        [
            ($"{rootPath}.zaaktype", rootPath),
            ($"{rootPath}.zaaktype.catalogus", $"{rootPath}.zaaktype"),
            ($"{rootPath}.status", rootPath),
            ($"{rootPath}.status.statustype", $"{rootPath}.status"),
            ($"{rootPath}.resultaat", rootPath),
            ($"{rootPath}.resultaat.resultaattype", $"{rootPath}.resultaat"),
            ($"{rootPath}.rollen", rootPath),
            ($"{rootPath}.rollen.roltype", $"{rootPath}.rollen"),
            ($"{rootPath}.zaakobjecten", rootPath),
            ($"{rootPath}.zaakobjecten.zaakobjecttype", $"{rootPath}.zaakobjecten"),
            ($"{rootPath}.zaakinformatieobjecten", rootPath),
            ($"{rootPath}.zaakinformatieobjecten.informatieobject", $"{rootPath}.zaakinformatieobjecten"),
        ];

    /// <summary>
    /// Strips <paramref name="rootPath"/>'s own dotted prefix off every requested path nested under it
    /// (e.g. "hoofdzaak.status" -> "status"), for forwarding to the reused
    /// <see cref="ExpandEngine{TEntity}"/>. Shared by <see cref="ZaakHoofdzaakResolver"/> and
    /// <see cref="ZaakDeelzakenResolver"/> for the same reason <see cref="Build"/> is.
    /// </summary>
    public static List<string> ExtractNestedPaths(IReadOnlySet<string> requestedPaths, string rootPath) =>
        requestedPaths.Where(p => p.StartsWith($"{rootPath}.", StringComparison.Ordinal)).Select(p => p[(rootPath.Length + 1)..]).ToList();
}
