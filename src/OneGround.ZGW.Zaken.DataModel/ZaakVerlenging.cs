using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

[Table("zaakverlengingen")]
public class ZaakVerlenging : OwnedEntity, IAuditableEntity
{
    [Key, ForeignKey(nameof(Zaak))]
    [Column("zaak_id")]
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

    public Zaak Zaak { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("reden")]
    public string Reden { get; set; }

    [Required]
    [Column("duur")]
    public Period Duur { get; set; }
}
