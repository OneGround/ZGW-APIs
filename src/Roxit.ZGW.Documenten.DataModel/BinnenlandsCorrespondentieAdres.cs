using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.Documenten.DataModel;

public class BinnenlandsCorrespondentieAdres
{
    [MaxLength(1)]
    [Column("huisletter")]
    public string Huisletter { get; set; }

    [Column("huisnummer")]
    public int Huisnummer { get; set; }

    [MaxLength(4)]
    [Column("huisnummertoevoeging")]
    public string HuisnummerToevoeging { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("naamopenbareruimte")]
    public string NaamOpenbareRuimte { get; set; }

    [MaxLength(6)]
    [Column("postcode")]
    public string Postcode { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("woonplaatsnaam")]
    public string WoonplaatsNaam { get; set; }
}
