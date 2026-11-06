using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

/// <summary>
/// One CloudEvent of the event stream. Written in the same database transaction as the change it describes
/// (outbox), so the stream never misses a change and never shows one that was rolled back.
/// </summary>
[Table("events")]
public class GesprekEventRecord : OwnedEntity
{
    /// <summary>
    /// Position in the stream; the SSE id and the cursor clients resume from.
    /// </summary>
    [Key]
    [Column("sequence")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Sequence { get; set; }

    [Column("event_id")]
    public Guid EventId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("type")]
    public string Type { get; set; }

    [Column("gesprek_id")]
    public Guid GesprekId { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("subject")]
    public string Subject { get; set; }

    [MaxLength(1000)]
    [Column("aanleidingzaak")]
    public string AanleidingZaak { get; set; }

    [Column("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// True when the content was removed from the history (redaction). The sequence stays, so the oldest available
    /// cursor does not move; the stream skips the event.
    /// </summary>
    [Column("geredigeerd")]
    public bool Geredigeerd { get; set; }

    /// <summary>
    /// The resource after the change, as JSON.
    /// </summary>
    [Required]
    [Column("data", TypeName = "jsonb")]
    public string Data { get; set; }
}
