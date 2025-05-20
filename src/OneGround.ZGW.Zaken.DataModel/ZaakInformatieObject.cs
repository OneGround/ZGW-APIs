using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

[Table("zaakinformatieobjecten")]
public class ZaakInformatieObject : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/zaakinformatieobjecten/{Id}";

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
    [Column("informatieobject")]
    public string InformatieObject { get; set; }

    [Column("aardrelatieweergave", TypeName = "smallint")]
    public AardRelatieWeergave AardRelatieWeergave { get; set; }

    [Column("registratiedatum")]
    public DateTime RegistratieDatum { get; set; }

    [MaxLength(200)]
    [Column("titel")]
    public string Titel { get; set; }

    [Column("beschrijving")]
    public string Beschrijving { get; set; }

    [Column("vernietigingsdatum")]
    public DateTime? VernietigingsDatum { get; set; }

    [Column("status_id")]
    public Guid? StatusId { get; set; }

    [ForeignKey("StatusId")]
    public ZaakStatus Status { get; set; }
}
