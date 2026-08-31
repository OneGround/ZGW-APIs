using System;
using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetZaakSnapshotsQueryParameters
{
    // 'after' is de id van een eerder teruggegeven snapshot-item (de primary key van de
    // onderliggende audittrail_deltas-rij) — het aantal snapshots kan te groot zijn om altijd
    // in één keer terug te geven, dus ook _snapshots ondersteunt cursor-gebaseerde paginatie.
    [FromQuery(Name = "after")]
    public Guid? After { get; set; }

    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }

    // Filtert op de aanmaakdatum van de onderliggende audittrail-rij (>=) — het totaal aantal
    // snapshots kan zo groot zijn dat je alleen geïnteresseerd bent in bijv. alles vanaf 2026.
    [FromQuery(Name = "aanmaakdatum__gte")]
    public DateTime? AanmaakdatumGte { get; set; }
}
