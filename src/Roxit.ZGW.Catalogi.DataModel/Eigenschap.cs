using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("eigenschappen")]
public class Eigenschap : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/eigenschappen/{Id}";

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
    [MaxLength(20)]
    [Column("namm")]
    public string Naam { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("definitie")]
    public string Definitie { get; set; }

    public EigenschapSpecificatie Specificatie { get; set; }

    [MaxLength(1000)]
    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey("ZaakTypeId")]
    public ZaakType ZaakType { get; set; }

    public List<StatusTypeVerplichteEigenschap> StatusTypeVerplichtEigenschappen { get; set; }

    [Column("statustype_id")]
    public Guid? StatusTypeId { get; set; }

    [ForeignKey("StatusTypeId")]
    public StatusType StatusType { get; set; }

    [Column("begingeldigheid")]
    public DateOnly? BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }
}
