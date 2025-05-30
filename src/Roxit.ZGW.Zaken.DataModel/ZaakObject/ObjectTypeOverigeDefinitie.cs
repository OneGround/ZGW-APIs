using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakObject;

[Table("objecttype_overige_definities")]
public class ObjectTypeOverigeDefinitie : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("url")]
    public string Url { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("schema")]
    public string Schema { get; set; }

    [Required]
    [Column("objectdata")]
    [MaxLength(100)]
    public string ObjectData { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
