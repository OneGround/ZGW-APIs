using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel;

[Table("zaakresultaten")]
public class ZaakResultaat : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/resultaten/{Id}";

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

    [Column("zaak_id")]
    public Guid ZaakId { get; set; }

    [ForeignKey("ZaakId")]
    public Zaak Zaak { get; set; }

    [Column("resultaattype")]
    [MaxLength(1000)]
    public string ResultaatType { get; set; }

    [Column("toelichting")]
    [MaxLength(1000)]
    public string Toelichting { get; set; }
}
