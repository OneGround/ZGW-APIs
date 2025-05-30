using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("referentieprocessen")]
public class ReferentieProces : IBaseEntity
{
    [Key, ForeignKey(nameof(ZaakType))]
    [Column("zaaktype_id")]
    public Guid Id { get; set; }

    public ZaakType ZaakType { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("naam")]
    public string Naam { get; set; }

    [MaxLength(200)]
    [Column("link")]
    public string Link { get; set; }
}
