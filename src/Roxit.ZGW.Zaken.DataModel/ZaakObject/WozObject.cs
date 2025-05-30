using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakObject;

[Table("woz_objecten")]
public class WozObject : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("wozobjectnummer")]
    public string WozObjectNummer { get; set; }

    [Column("aanduidingwozobject_id")]
    public Guid? AanduidingWozObjectId { get; set; }

    [ForeignKey(nameof(AanduidingWozObjectId))]
    public AanduidingWozObject AanduidingWozObject { get; set; }
}
