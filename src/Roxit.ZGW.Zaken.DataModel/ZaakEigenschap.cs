using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel;

[Table("zaakeigenschappen")]
public class ZaakEigenschap : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"{Zaak?.Url}/zaakeigenschappen/{Id}";

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
    [MaxLength(1000)]
    [Column("eigenschap")]
    public string Eigenschap { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("naam")]
    public string Naam { get; set; }

    [Required]
    [Column("waarde")]
    public string Waarde { get; set; }
}
