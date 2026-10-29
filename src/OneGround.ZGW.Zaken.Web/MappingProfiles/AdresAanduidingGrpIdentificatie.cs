using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles;

/// <summary>
/// The identificatie of the adresAanduidingGrp of a terrein_gebouwd_object zaakobject has two names: the misspelled (deprecated) oaoIdentificatie and
/// the correct aoaIdentificatie (#2576). A response returns both with the stored value. A request may use either, so which one holds the value the
/// client means has to be decided here, in one place:
/// <list type="bullet">
/// <item>Only one name is set: that one.</item>
/// <item>Both are set and equal: that value.</item>
/// <item>Both are set and differ: when an object is created, aoaIdentificatie wins. When it is updated (PUT or PATCH) the stored value is known, and
/// the name that differs from it is the one the client changed: a client that does GET, changes only oaoIdentificatie and sends the whole body back
/// still sends the old value under aoaIdentificatie. When both differ from the stored value, aoaIdentificatie wins.</item>
/// </list>
/// The stored value stays in the one existing column.
/// </summary>
public static class AdresAanduidingGrpIdentificatie
{
    /// <summary>Fills what a request maps to: the value to store when the object is created, and what was sent under each name.</summary>
    public static void Apply(TerreinGebouwdObjectZaakObject target, AdresAanduidingGrpDto source)
    {
        target.AdresAanduidingGrp_OaoIdentificatie = ResolveForCreate(source?.OaoIdentificatie, source?.AoaIdentificatie);
        target.AdresAanduidingGrp_OaoIdentificatieAsSent = source?.OaoIdentificatie;
        target.AdresAanduidingGrp_AoaIdentificatieAsSent = source?.AoaIdentificatie;
    }

    // Note: a blank name counts as not sent, so it never wins over a name that has a value
    public static string ResolveForCreate(string oaoIdentificatie, string aoaIdentificatie) =>
        !string.IsNullOrWhiteSpace(aoaIdentificatie) ? aoaIdentificatie
        : !string.IsNullOrWhiteSpace(oaoIdentificatie) ? oaoIdentificatie
        : aoaIdentificatie ?? oaoIdentificatie;

    /// <param name="request">The object mapped from the request (see <see cref="Apply"/>).</param>
    /// <param name="stored">The value that is stored now.</param>
    public static string ResolveForUpdate(TerreinGebouwdObjectZaakObject request, string stored)
    {
        var oao = request.AdresAanduidingGrp_OaoIdentificatieAsSent;
        var aoa = request.AdresAanduidingGrp_AoaIdentificatieAsSent;

        // Note: a blank name counts as not sent: it must never win over the stored value
        if (!string.IsNullOrWhiteSpace(oao) && !string.IsNullOrWhiteSpace(aoa) && oao != aoa)
        {
            if (oao == stored)
            {
                return aoa;
            }

            if (aoa == stored)
            {
                return oao;
            }

            return aoa;
        }

        // Note: also for a request object that was not mapped from a dto: then only the resolved value is known.
        // A blank value counts as not sent here too, so it keeps the stored value
        var resolved = request.AdresAanduidingGrp_OaoIdentificatie;

        return string.IsNullOrWhiteSpace(resolved) ? stored : resolved;
    }
}
