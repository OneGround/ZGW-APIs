using System.ComponentModel.DataAnnotations.Schema;

namespace OneGround.ZGW.Catalogi.DataModel;

// Note: Not mapped entity anymore. This is used to resove soft relations between besluittype and zaaktype [Bt->Zt'n]

public class BesluitTypeZaakType
{
    [NotMapped]
    public BesluitType BesluitType { get; set; }

    [NotMapped]
    public string ZaakTypeIdentificatie { get; set; }

    [NotMapped]
    public ZaakType ZaakType { get; set; }
}
