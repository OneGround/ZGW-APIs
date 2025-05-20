using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_gemeenten")]
public class GemeenteZaakObject : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("gemeentenaam")]
    public string GemeenteNaam { get; set; }

    [Required]
    [MaxLength(4)]
    [Column("gemeentecode")]
    public string GemeenteCode { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
