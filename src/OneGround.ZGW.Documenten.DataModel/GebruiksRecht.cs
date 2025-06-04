using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.DataModel;

[Table("gebruiksrechten")]
public class GebruiksRecht : IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/gebruiksrechten/{Id}";

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
    [Column("omschrijvingvoorwaarden")]
    public string OmschrijvingVoorwaarden { get; set; }

    [Column("startdatum")]
    public DateTime Startdatum { get; set; }

    [Column("einddatum")]
    public DateTime? Einddatum { get; set; }

    [Column("informatieobject_id")]
    public Guid InformatieObjectId { get; set; }

    [ForeignKey(nameof(InformatieObjectId))]
    public EnkelvoudigInformatieObject InformatieObject { get; set; }
}
