using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakRol;

[Table("zaakrollen_niet_natuurlijk_personen")]
public class NietNatuurlijkPersoonZaakRol : OwnedEntity, IAuditableEntity
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

    [MaxLength(9)]
    [Column("innnnpid")]
    public string InnNnpId { get; set; }

    [MaxLength(17)]
    [Column("annidentificatie")]
    public string AnnIdentificatie { get; set; }

    [MaxLength(500)]
    [Column("statutairenaam")]
    public string StatutaireNaam { get; set; }

    [Column("innrechtsvorm", TypeName = "smallint")]
    public InnRechtsvorm? InnRechtsvorm { get; set; }

    [MaxLength(1000)]
    [Column("bezoekadres")]
    public string Bezoekadres { get; set; }

    [Column("subverblijfbuitenland_id")]
    public Guid? SubVerblijfBuitenlandId { get; set; }

    [ForeignKey("SubVerblijfBuitenlandId")]
    public SubVerblijfBuitenland SubVerblijfBuitenland { get; set; }

    [Column("zaakrol_id")]
    public Guid ZaakRolId { get; set; }

    [ForeignKey("ZaakRolId")]
    public ZaakRol ZaakRol { get; set; }
}
