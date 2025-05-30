using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.DataAccess.AuditTrail;

[Table("audittrail")]
public class AuditTrailRegel : IBaseEntity
{
    [NotMapped]
    public string Url => HoofdObject;

    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("bron")]
    public string Bron { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("applicatie_id")]
    public string ApplicatieId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("applicatieweergave")]
    public string ApplicatieWeergave { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("gebruikers_id")]
    public string GebruikersId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("gebruikersweergave")]
    public string GebruikersWeergave { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("actie")]
    public string Actie { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("actieweergave")]
    public string ActieWeergave { get; set; }

    [Column("resultaat")]
    public int Resultaat { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("hoofdobject")]
    public string HoofdObject { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("resource")]
    public string Resource { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("resourceurl")]
    public string ResourceUrl { get; set; }

    [Required]
    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("resourceweergave")]
    public string ResourceWeergave { get; set; }

    [Column("aanmaakdatum")]
    public DateTime AanmaakDatum { get; set; }

    [Column("oud", TypeName = "jsonb")]
    public string Oud { get; set; }

    [Column("nieuw", TypeName = "jsonb")]
    public string Nieuw { get; set; }

    [MaxLength(255)]
    [Column("request_id")]
    public string RequestId { get; set; }

    [Column("hoofdobject_id")]
    public Guid? HoofdObjectId { get; set; }
}
