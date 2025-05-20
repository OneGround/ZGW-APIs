using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneGround.ZGW.Documenten.DataModel;

public class CorrespondentiePostadres
{
    [Column("postbusofantwoordnummer")]
    public int PostbusOfAntwoordnummer { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("postadrespostcode")]
    public string PostadresPostcode { get; set; }

    [Required]
    [Column("postadrestype")]
    public PostadresType PostadresType { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("woonplaatsnaam")]
    public string WoonplaatsNaam { get; set; }
}
