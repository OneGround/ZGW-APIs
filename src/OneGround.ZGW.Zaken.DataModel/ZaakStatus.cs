using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

public interface IZaakEntity
{
    public Zaak Zaak { get; }
}

[Table("zaakstatussen")]
public class ZaakStatus : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/statussen/{Id}";

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
    [Column("statustype")]
    public string StatusType { get; set; }

    [Required]
    [Column("datumstatusgezet")]
    public DateTime DatumStatusGezet { get; set; }

    [MaxLength(1000)]
    [Column("statustoelichting")]
    public string StatusToelichting { get; set; }

    [Column("indicatielaatstgezettestatus")]
    public bool? IndicatieLaatstGezetteStatus { get; set; }

    [MaxLength(200)]
    [Column("gezetdoor")]
    public string GezetDoor { get; set; }
}
