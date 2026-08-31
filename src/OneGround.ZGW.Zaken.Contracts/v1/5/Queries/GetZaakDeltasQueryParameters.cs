using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

public class GetZaakDeltasQueryParameters
{
    // 'after' is de primary key (id) van een audittrail_deltas-rij, dezelfde id als in de
    // _snapshots-response — niet een berekende positie. Verplicht: _deltas is het vervolg van de
    // versie-geschiedenis van de resource waar deze id bij hoort (niet een collectie-brede stream),
    // dus zonder 'after' is er geen resource om op te scopen.
    [Required]
    [FromQuery(Name = "after")]
    public Guid? After { get; set; }

    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }
}
