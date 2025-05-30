using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("zaaktypegerelateerdezaaktypen")]
public class ZaakTypeGerelateerdeZaakType : OwnedEntity, IAuditableEntity
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

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey(nameof(ZaakTypeId))]
    public ZaakType ZaakType { get; set; }

    [Column("aardrelatie", TypeName = "smallint")]
    public AardRelatie AardRelatie { get; set; }

    [MaxLength(255)]
    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("gerelateerdezaaktype_identificatie")]
    [MaxLength(80)]
    public string GerelateerdeZaakTypeIdentificatie { get; set; }

    // Note: Not mapped field because this is a soft relation between zaaktype and deelzaaktype
    //   (soft relation is mapped: zaaktype.identificatie <-> deelzaaktype(v1_3).deelzaaktype_identificatie within geldigheid)
    [NotMapped]
    public ZaakType GerelateerdeZaakType { get; set; }
}
