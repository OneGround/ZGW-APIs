using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("roltypen")]
public class RolType : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/roltypen/{Id}";

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
    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey("ZaakTypeId")]
    public virtual ZaakType ZaakType { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("omschrijving")]
    public string Omschrijving { get; set; }

    [Required]
    [Column("omschrijvinggeneriek")]
    public Common.DataModel.OmschrijvingGeneriek OmschrijvingGeneriek { get; set; }

    [Column("begingeldigheid")]
    public DateOnly? BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }
}
