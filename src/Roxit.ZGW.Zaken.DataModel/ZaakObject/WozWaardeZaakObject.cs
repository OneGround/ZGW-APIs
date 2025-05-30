using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_woz_waarden")]
public class WozWaardeZaakObject : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("waardepeildatum")]
    public string WaardePeildatum { get; set; }

    [Column("woz_object_id")]
    public Guid? IsVoorId { get; set; }

    [ForeignKey(nameof(IsVoorId))]
    public WozObject IsVoor { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
