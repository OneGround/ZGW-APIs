using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_overigen")]
public class OverigeZaakObject : OwnedEntity, IAuditableEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("creationtime")]
    public DateTime CreationTime { get; set; }

    [Column("modificationtime")]
    public DateTime? ModificationTime { get; set; }

    [MaxLength(50)]
    [Column("createdby")]
    public string CreatedBy { get; set; }

    [MaxLength(50)]
    [Column("modifiedby")]
    public string ModifiedBy { get; set; }

    [Required]
    [Column("overigedata")]
    public string OverigeData { get; set; }

    [Column("overigedata_jsonb", TypeName = "jsonb")]
    public string OverigeDataJsonb { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }
}
