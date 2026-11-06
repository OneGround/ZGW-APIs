using System;
using OneGround.ZGW.Gesprekken.DataModel;

namespace OneGround.ZGW.Gesprekken.Web.Models.v1;

public class GetAllGesprekkenFilter
{
    public string Referentie { get; set; }
    public string AanleidingZaak { get; set; }
    public string AanleidingPlan { get; set; }
    public DateTime? AanvangsmomentGesprekGte { get; set; }
    public DateTime? AanvangsmomentGesprekLte { get; set; }
    public string GespreksdeelnamePartij { get; set; }
    public string GespreksdeelnameActor { get; set; }
    public Guid? Gespreksdeelname { get; set; }
    public DeelnameType? GespreksdeelnameType { get; set; }
}
