using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.DataModel;

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

    [NotMapped] // ZZZ
    [Column("informatieobject_id")]
    public Guid InformatieObjectId { get; set; }

    [NotMapped] // ZZZ
    [ForeignKey("InformatieObjectId")]
    public EnkelvoudigInformatieObject InformatieObject { get; set; }

    // ZZZ
    [Column("informatieobject_id2")]
    public Guid InformatieObjectId2 { get; set; }

    [ForeignKey("informatieobject_id2")]
    public EnkelvoudigInformatieObject2 InformatieObject2 { get; set; }

    [Required]
    [Column("objecttype")]
    public ObjectType ObjectType { get; set; }
}
