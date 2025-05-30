using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Roxit.ZGW.Catalogi.Contracts.v1._3;

public class OmschrijvingGeneriekDto
{
    [JsonProperty("informatieobjecttypeOmschrijvingGeneriek")]
    public string InformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [JsonProperty("definitieInformatieobjecttypeOmschrijvingGeneriek")]
    public string DefinitieInformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [JsonProperty("herkomstInformatieobjecttypeOmschrijvingGeneriek")]
    public string HerkomstInformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [JsonProperty("hierarchieInformatieobjecttypeOmschrijvingGeneriek")]
    public string HierarchieInformatieObjectTypeOmschrijvingGeneriek { get; set; }

    [Column("opmerkingInformatieobjecttypeOmschrijvingGeneriek")]
    public string OpmerkingInformatieObjectTypeOmschrijvingGeneriek { get; set; }
}
