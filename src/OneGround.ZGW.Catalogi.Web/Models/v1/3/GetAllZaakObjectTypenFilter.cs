using System;

namespace OneGround.ZGW.Catalogi.Web.Models.v1._3;

public class GetAllZaakObjectTypenFilter
{
    public bool? AnderObjectType { get; set; }
    public string Catalogus { get; set; }
    public DateOnly? DatumBeginGeldigheid { get; set; }
    public DateOnly? DatumEindeGeldigheid { get; set; }
    public DateOnly? DatumGeldigheid { get; set; }
    public string ObjectType { get; set; }
    public string RelatieOmschrijving { get; set; }
    public string ZaakType { get; set; }
    public string ZaaktypeIdentificatie { get; set; }
}
