using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

// Note: The values are the defaults of a new ZAAKNOTITIE when notitieType/status is not given (0 = intern/concept)
public enum ZaakNotitieType
{
    intern,
    @extern,
}

public enum ZaakNotitieStatus
{
    concept,
    definitief,
}

[Table("zaaknotities")]
public class ZaakNotitie : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/zaaknotities/{Id}";

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

    [Required]
    [MaxLength(255)]
    [Column("onderwerp")]
    public string Onderwerp { get; set; }

    [Required]
    [Column("tekst")]
    public string Tekst { get; set; }

    [MaxLength(255)]
    [Column("aangemaaktdoor")]
    public string AangemaaktDoor { get; set; }

    [Column("notitietype")]
    public ZaakNotitieType NotitieType { get; set; }

    [Column("status")]
    public ZaakNotitieStatus Status { get; set; }
}
