using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.Catalogi.DataModel;

// Note: De CATALOGUS waaraan het ZAAKTYPE is ontleend.
public class BronCatalogus
{
    [Column("url")]
    public string Url { get; set; }

    [Column("domein")]
    public string Domein { get; set; }

    [Column("rsin")]
    public string Rsin { get; set; }
}
