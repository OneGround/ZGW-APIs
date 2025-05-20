using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_kadastrale_onroerende_zaken")]
public class KadastraleOnroerendeZaakObject : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("kadastraleidentificatie")]
    public string KadastraleIdentificatie { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("kadastraleaanduiding")]
    public string KadastraleAanduiding { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
