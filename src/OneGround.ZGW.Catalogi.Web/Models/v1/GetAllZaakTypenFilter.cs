using System;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.Web.Models.v1;

public class GetAllZaakTypenFilter
{
    public string Catalogus { get; set; }

    public string Identificatie { get; set; }

    public string[] Trefwoorden { get; set; }

    public ConceptStatus Status { get; set; } = ConceptStatus.definitief;

    public DateOnly? DatumGeldigheid { get; set; }
}
