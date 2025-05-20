using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakRol;

[Table("zaakrollen_vestigingen")]
public class VestigingZaakRol : OwnedEntity, IAuditableEntity
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

    [MaxLength(24)]
    [Column("vestigingsnummer")]
    public string VestigingsNummer { get; set; }

    [Column("handelsnaam")]
    public List<string> Handelsnaam { get; set; }

    [Column("verblijfsadres_id")]
    public Guid? VerblijfsadresId { get; set; }

    [ForeignKey("VerblijfsadresId")]
    public Verblijfsadres Verblijfsadres { get; set; }

    [Column("subverblijfbuitenland_id")]
    public Guid? SubVerblijfBuitenlandId { get; set; }

    [ForeignKey("SubVerblijfBuitenlandId")]
    public SubVerblijfBuitenland SubVerblijfBuitenland { get; set; }

    [Column("zaakrol_id")]
    public Guid ZaakRolId { get; set; }

    [ForeignKey("ZaakRolId")]
    public ZaakRol ZaakRol { get; set; }

    [MaxLength(8)]
    [Column("kvknummer")]
    public string KvkNummer { get; set; }
}
