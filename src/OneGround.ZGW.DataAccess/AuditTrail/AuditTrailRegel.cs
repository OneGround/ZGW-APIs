using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace OneGround.ZGW.DataAccess.AuditTrail;

public interface IZgwAuditTrailRegel : IBaseEntity
{
    public string Url { get; }
    public string Bron { get; set; }
    public string ApplicatieId { get; set; }
    public string ApplicatieWeergave { get; set; }
    public string GebruikersId { get; set; }
    public string GebruikersWeergave { get; set; }
    public string Actie { get; set; }
    public string ActieWeergave { get; set; }
    public int Resultaat { get; set; }
    public string HoofdObject { get; set; }
    public string Resource { get; set; }
    public string ResourceUrl { get; set; }
    public string Toelichting { get; set; }
    public string ResourceWeergave { get; set; }
    public DateTime AanmaakDatum { get; set; }
    public string RequestId { get; set; }
    public Guid? HoofdObjectId { get; set; }
}

[Table("audittrail")]
public class AuditTrailRegel : IZgwAuditTrailRegel
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

[Table("audittrail_deltas")]
public class AuditTrailDelta : IZgwAuditTrailRegel
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

    [Column("delta_json", TypeName = "jsonb")]
    public string DeltaJson { get; set; }

    [Column("snapshot_json", TypeName = "jsonb")]
    public string SnapshotJson { get; set; }

    [Column("versie")]
    public int Versie { get; set; }

    [MaxLength(255)]
    [Column("request_id")]
    public string RequestId { get; set; }

    [Column("hoofdobject_id")]
    public Guid? HoofdObjectId { get; set; }

    [Column("resource_id")]
    public Guid? ResourceId { get; set; }
}
