using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.Catalogi.DataModel;

// Note: Het zaaktype binnen de CATALOGUS waaraan dit ZAAKTYPE is ontleend.
public class BronZaaktype
{
    [Column("url")]
    public string Url { get; set; }

    [Column("identificatie")]
    public string Identificatie { get; set; }

    [Column("omschrijving")]
    public string Omschrijving { get; set; }
}
