using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Notificaties.DataModel;

[Table("filtervalues")]
public class FilterValue : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("key")]
    public string Key { get; set; }

    [MaxLength(1000)]
    [Column("value")]
    public string Value { get; set; }

    [Column("abonnement_kanaal_id")]
    public Guid AbonnementKanaalId { get; set; }

    [ForeignKey("AbonnementKanaalId")]
    public AbonnementKanaal AbonnementKanaal { get; set; }
}
