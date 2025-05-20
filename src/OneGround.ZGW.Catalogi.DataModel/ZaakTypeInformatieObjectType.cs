using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("zaaktypeinformatieobjecttypen")]
public class ZaakTypeInformatieObjectType : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/zaaktype-informatieobjecttypen/{Id}";

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
    [MaxLength(50)]
    [Column("richting")]
    public Richting Richting { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("volgnummer")]
    public int VolgNummer { get; set; }

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey("ZaakTypeId")]
    public virtual ZaakType ZaakType { get; set; }

    [Column("informatieobjecttype_omschrijving")]
    [MaxLength(100)]
    public string InformatieObjectTypeOmschrijving { get; set; }

    [Column("statustype_id")]
    public Guid? StatusTypeId { get; set; }

    [ForeignKey("StatusTypeId")]
    public virtual StatusType StatusType { get; set; }

    // Note: Not mapped field because this is a soft relation between zaaktype and informatieobjecttype
    //   (soft relation is mapped: informatieobjecttype.omschrijving <-> zaaktypeinformatieobjecttype(v1_3).informatieobjecttype_omschrijving within geldigheid)
    [NotMapped]
    public InformatieObjectType InformatieObjectType { get; set; }
}
