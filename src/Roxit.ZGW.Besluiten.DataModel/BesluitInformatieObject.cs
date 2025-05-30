using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Besluiten.DataModel;

[Table("besluitinformatieobjecten")]
public class BesluitInformatieObject : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/besluitinformatieobjecten/{Id}";

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

    [Column("aardrelatie")]
    public AardReleatie AardRelatie { get; set; }

    [Column("registratiedatum")]
    public DateOnly Registratiedatum { get; set; }

    [Column("besluit_id")]
    public Guid BesluitId { get; set; }

    [ForeignKey(nameof(BesluitId))]
    public Besluit Besluit { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("informatieobject")]
    public string InformatieObject { get; set; }
}
