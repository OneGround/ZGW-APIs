using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneGround.ZGW.Documenten.DataModel;

public class BuitenlandsCorrespondentieAdres
{
    [Required]
    [MaxLength(35)]
    [Column("adresbuitenland1")]
    public string AdresBuitenland1 { get; set; }

    [MaxLength(35)]
    [Column("adresbuitenland2")]
    public string AdresBuitenland2 { get; set; }

    [MaxLength(35)]
    [Column("adresbuitenland3")]
    public string AdresBuitenland3 { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("landpostadres")]
    public string LandPostadres { get; set; }
}
