using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

public enum BestandStatus
{
    uploadInAfwachting,
    beschikbaar,
    geweigerd,
    verlopen,
}

/// <summary>
/// Een bestand voor een bijlage. De inhoud staat in de objectopslag; de client uploadt en downloadt daar rechtstreeks
/// met een tijdelijke URL.
/// </summary>
[Table("bestanden")]
public class Bestand : AuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/bestanden/{Id}";

    [Required]
    [MaxLength(255)]
    [Column("bestandsnaam")]
    public string Bestandsnaam { get; set; }

    [Column("bestandsomvang")]
    public long Bestandsomvang { get; set; }

    [MaxLength(100)]
    [Column("contenttype")]
    public string ContentType { get; set; }

    [MaxLength(200)]
    [Column("titel")]
    public string Titel { get; set; }

    [Column("status")]
    public BestandStatus Status { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("sleutel")]
    public string Sleutel { get; set; }

    [Column("aangemaaktop")]
    public DateTime AangemaaktOp { get; set; }

    [Column("verlooptop")]
    public DateTime VerlooptOp { get; set; }

    [Column("geuploadop")]
    public DateTime? GeuploadOp { get; set; }
}
