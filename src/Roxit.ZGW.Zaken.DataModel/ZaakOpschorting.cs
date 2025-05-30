using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel;

[Table("zaakopschortingen")]
public class ZaakOpschorting : IBaseEntity
{
    [Key, ForeignKey(nameof(Zaak))]
    [Column("zaak_id")]
    public Guid Id { get; set; }

    public Zaak Zaak { get; set; }

    [Column("indicatie")]
    public bool Indicatie { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("reden")]
    public string Reden { get; set; }
}
