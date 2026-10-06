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
    /// Builds the nested "hoofdzaak.deelzaken.*" tuples -- deelzaken-of-hoofdzaak, one level deeper than
    /// <see cref="Build"/>'s own "hoofdzaak.*" graph. Deliberately narrower than <see cref="Build"/>:
    /// only zaaktype/status/resultaat, not rollen/zaakobjecten/zaakinformatieobjecten, and none of those
    /// three go one level deeper either (no "...zaaktype.catalogus", "...status.statustype",
    /// "...resultaat.resultaattype") -- that would be a 4th nesting level, exceeding the VNG ZGW spec's
    /// 3-level expand cap. Scoped to "hoofdzaak" only (not "deelzaken" or "relevanteanderezaken") because
    /// that's what was actually requested -- "deelzaken.deelzaken.*" (deelzaken-of-a-deelzaak) and
    /// "relevanteanderezaken.deelzaken.*" are plausible future asks but weren't asked for, so they
    /// aren't added speculatively; callers needing that parity should call this method with those root
    /// paths too (it already takes <paramref name="rootPath"/> as a parameter for exactly that reason).
    /// The "{rootPath}.deelzaken" tuple itself is required too, not just its children: without it, the
    /// parent-chain walk in <see cref="ExpandValidator{TEntity}"/>/<see cref="ExpandEngine{TEntity}"/>
    /// would never reach back to <paramref name="rootPath"/> (whose own <c>Parent</c> is <c>null</c>,
    /// terminating the walk), so <paramref name="rootPath"/> itself would never get dispatched.
    /// </summary>
    public static IEnumerable<(string Path, string Parent)> BuildDeelzakenUnder(string rootPath) =>
        [
            ($"{rootPath}.deelzaken", rootPath),
            ($"{rootPath}.deelzaken.zaaktype", $"{rootPath}.deelzaken"),
            ($"{rootPath}.deelzaken.status", $"{rootPath}.deelzaken"),
            ($"{rootPath}.deelzaken.resultaat", $"{rootPath}.deelzaken"),
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
