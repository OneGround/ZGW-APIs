using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_buurten")]
public class BuurtZaakObject : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(2)]
    [Column("buurtcode")]
    public string BuurtCode { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("buurtnaam")]
    public string BuurtNaam { get; set; }

    [Required]
    [MaxLength(4)]
    [Column("gemgemeentecode")]
    public string GemGemeenteCode { get; set; }

    [Required]
    [Column("wykwijkcode")]
    [MaxLength(2)]
    public string WykWijkCode { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
