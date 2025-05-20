using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

[Table("klantcontacten")]
public class KlantContact : IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/klantcontacten/{Id}";

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

    [MaxLength(14)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [Required]
    [Column("datum_tijd")]
    public DateTime DatumTijd { get; set; }

    [MaxLength(20)]
    [Column("kanaal")]
    public string Kanaal { get; set; }

    [MaxLength(200)]
    [Column("onderwerp")]
    public string Onderwerp { get; set; }

    [MaxLength(1000)]
    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("zaak_id")]
    public Guid ZaakId { get; set; }

    [ForeignKey("ZaakId")]
    public virtual Zaak Zaak { get; set; }
}
