using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Notificaties.DataModel;

[Table("abonnementkanalen")]
public class AbonnementKanaal : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("kanaal_id")]
    public Guid KanaalId { get; set; }

    [ForeignKey("KanaalId")]
    public Kanaal Kanaal { get; set; }

    [Column("abonnement_id")]
    public Guid AbonnementId { get; set; }

    [ForeignKey("AbonnementId")]
    public Abonnement Abonnement { get; set; }

    public IList<FilterValue> Filters { get; set; }
}
