using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Documenten.DataModel;

[Table("objectinformatieobjecten")]
public class ObjectInformatieObject : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/objectinformatieobjecten/{Id}";

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
    [MaxLength(200)]
    [Column("object")]
    public string Object { get; set; }

    [Column("informatieobject_id")]
    public Guid InformatieObjectId { get; set; }

    [ForeignKey("InformatieObjectId ")]
    public EnkelvoudigInformatieObject InformatieObject { get; set; }

    [Required]
    [Column("objecttype")]
    public ObjectType ObjectType { get; set; }
}
