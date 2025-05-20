using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneGround.ZGW.Catalogi.DataModel;

public class CheckListItemStatusType
{
    [Required]
    [Column("itemnaam")]
    public string ItemNaam { get; set; }

    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Required]
    [Column("vraagstelling")]
    public string Vraagstelling { get; set; }

    [Column("verplicht")]
    public bool Verplicht { get; set; }
}
