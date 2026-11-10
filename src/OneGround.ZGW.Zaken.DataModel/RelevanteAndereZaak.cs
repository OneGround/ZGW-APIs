using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

[Table("relevanteanderezaken")]
public class RelevanteAndereZaak : OwnedEntity, IAuditableEntity
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

    // Note: Superseded by GerelateerdeZaakId. Once all existing rows have been migrated (DataMigrator) this column can be phased out;
    // the url is then built from GerelateerdeZaakId.
    [Required]
    [MaxLength(1000)]
    [Column("url")]
    public string Url { get; set; }

    [Column("zaak_id")]
    public Guid ZaakId { get; set; }

    [ForeignKey("ZaakId")]
    public Zaak Zaak { get; set; }

    /// <summary>
    /// The zaak <see cref="Url"/> refers to. Deleting that zaak removes this relation (cascade); null until existing rows have been backfilled.
    /// </summary>
    [Column("gerelateerde_zaak_id")]
    public Guid? GerelateerdeZaakId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("aardrelatie")]
    public string AardRelatie { get; set; }
}
