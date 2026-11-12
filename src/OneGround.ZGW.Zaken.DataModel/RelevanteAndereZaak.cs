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

    // Note: Superseded by GerelateerdeZaakId: the url in responses is built from that id. This column is no longer read or written; it only
    // holds the url of rows that were created before. Planned to be dropped (column and property) in a following release.
    [Obsolete("No longer read or written: the relation is kept by GerelateerdeZaakId. The column will be dropped in a following release.")]
    [MaxLength(1000)]
    [Column("url")]
    public string Url { get; set; }

    [Column("zaak_id")]
    public Guid ZaakId { get; set; }

    [ForeignKey("ZaakId")]
    public Zaak Zaak { get; set; }

    /// <summary>
    /// The zaak this relation refers to. Deleting that zaak removes this relation (cascade).
    /// </summary>
    [Column("gerelateerde_zaak_id")]
    public Guid GerelateerdeZaakId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("aardrelatie")]
    public string AardRelatie { get; set; }
}
