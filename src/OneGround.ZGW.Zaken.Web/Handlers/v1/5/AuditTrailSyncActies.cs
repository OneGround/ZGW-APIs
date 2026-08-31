using OneGround.ZGW.Common.Web.Services.AuditTrail;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

// Note: gedeeld tussen GetZaakSnapshotsQueryHandler en GetZaakDeltasQueryHandler zodat beide
// hetzelfde begrip 'collectie-mutatie' hanteren voor de synchronisatie-endpoints.
internal static class AuditTrailSyncActies
{
    // Note: retrieve-acties worden hier bewust uitgesloten; die zijn geen collectie-mutatie.
    public static readonly string[] Mutaties =
    {
        $"{AuditActie.create}",
        $"{AuditActie.update}",
        $"{AuditActie.partial_update}",
        $"{AuditActie.destroy}",
    };
}
