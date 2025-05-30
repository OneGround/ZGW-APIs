using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Notificaties.DataModel;

[Table("abonnementen")]
public class Abonnement : OwnedEntity, IBaseEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/abonnement/{Id}";

    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("callbackurl")]
    [MaxLength(200)]
    public string CallbackUrl { get; set; }

    [Required]
    [Column("auth")]
    [MaxLength(1000)]
    public string Auth { get; set; }

    public IList<AbonnementKanaal> AbonnementKanalen { get; set; }
}
