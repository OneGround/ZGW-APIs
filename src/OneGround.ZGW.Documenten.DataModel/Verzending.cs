using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.DataModel;

[Table("verzendingen")]
public class Verzending : IAuditableEntity, IUrlEntity, IInformatieObjectEntity
{
    [NotMapped]
    public string Url => $"/verzendingen/{Id}";

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
    [MaxLength(200)]
    [Column("betrokkene")]
    public string Betrokkene { get; set; }

    [Column("aardrelatie", TypeName = "smallint")]
    public AardRelatie AardRelatie { get; set; }

    [MaxLength(200)]
    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("ontvangstdatum")]
    public DateOnly? Ontvangstdatum { get; set; }

    [Column("verzenddatum")]
    public DateOnly? Verzenddatum { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("contactpersoon")]
    public string Contactpersoon { get; set; }

    [MaxLength(40)]
    [Column("contactpersoonnaam")]
    public string ContactpersoonNaam { get; set; }

    [Column("binnenlandsCorrespondentieadres", TypeName = "jsonb")]
    public BinnenlandsCorrespondentieAdres BinnenlandsCorrespondentieAdres { get; set; }

    [Column("buitenlandsCorrespondentieadres", TypeName = "jsonb")]
    public BuitenlandsCorrespondentieAdres BuitenlandsCorrespondentieAdres { get; set; }

    [Column("correspondentiepostadres", TypeName = "jsonb")]
    public CorrespondentiePostadres CorrespondentiePostadres { get; set; }

    [MaxLength(15)]
    [Column("faxnummer")]
    public string Faxnummer { get; set; }

    [MaxLength(100)]
    [Column("emailadres")]
    public string EmailAdres { get; set; }

    [Column("mijnoverheid")]
    public bool MijnOverheid { get; set; }

    [MaxLength(15)]
    [Column("telefoonnummer")]
    public string Telefoonnummer { get; set; }

    [NotMapped] // ZZZ
    [Column("informatieobject_id")]
    public Guid InformatieObjectId { get; set; }

    [NotMapped] // ZZZ
    //[ForeignKey(nameof(InformatieObjectId))]
    public EnkelvoudigInformatieObject InformatieObject { get; set; }

    // ZZZ
    [Column("enkelvoudiginformatieobjectlock_id")]
    public Guid EnkelvoudigInformatieObjectLockId { get; set; }

    [ForeignKey("EnkelvoudigInformatieObjectLockId")]
    public EnkelvoudigInformatieObjectLock2 EnkelvoudigInformatieObjectLock { get; set; }
}
