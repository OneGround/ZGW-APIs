using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_adressen")]
public class AdresZaakObject : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("wplwoonplaatsnaam")]
    public string WplWoonplaatsNaam { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("goropenbareruimtenaam")]
    public string GorOpenbareRuimteNaam { get; set; }

    [Required]
    [Column("huisnummer")]
    public int Huisnummer { get; set; }

    [Column("huisletter")]
    [MaxLength(1)]
    public string Huisletter { get; set; }

    [MaxLength(4)]
    [Column("huisnummertoevoeging")]
    public string HuisnummerToevoeging { get; set; }

    [MaxLength(7)]
    [Column("postcode")]
    public string Postcode { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
