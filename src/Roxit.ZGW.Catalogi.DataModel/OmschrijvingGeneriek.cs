using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.Catalogi.DataModel;

public class OmschrijvingGeneriek
{
    [Column("informatieobjecttype_omschrijvinggeneriek")]
    public string InformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [Column("definitieinformatieobjecttype_omschrijvinggeneriek")]
    public string DefinitieInformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [Column("herkomstinformatieobjecttype_omschrijvinggeneriek")]
    public string HerkomstInformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [Column("hierarchieinformatieobjecttype_omschrijvinggeneriek")]
    public string HierarchieInformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [Column("opmerkinginformatieobjecttype_omschrijvinggeneriek")]
    public string OpmerkingInformatieObjectTypeOmschrijvingGeneriek { get; set; }
}
