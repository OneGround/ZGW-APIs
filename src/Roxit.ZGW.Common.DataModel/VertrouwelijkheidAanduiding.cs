namespace Roxit.ZGW.Common.DataModel;

/// <summary>
/// Aanduiding van de mate waarin zaakdossiers van ZAAKen van dit ZAAKTYPE voor de openbaarheid bestemd zijn.
/// Indien de zaak bij het aanmaken geen vertrouwelijkheidaanduiding krijgt, dan wordt deze waarde gezet.
/// </summary>
public enum VertrouwelijkheidAanduiding
{
    nullvalue = -1,
    openbaar,
    beperkt_openbaar,
    intern,
    zaakvertrouwelijk,
    vertrouwelijk,
    confidentieel,
    geheim,
    zeer_geheim,
}
