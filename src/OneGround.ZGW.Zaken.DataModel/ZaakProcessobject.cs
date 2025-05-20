using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

[Table("zaakprocessobjecten")]
public class ZaakProcessobject : IBaseEntity
{
    [Key, ForeignKey(nameof(Zaak))]
    [Column("zaak_id")]
    public Guid Id { get; set; }

    public Zaak Zaak { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("datumkenmerk")]
    public string Datumkenmerk { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("objecttype")]
    public string Objecttype { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("registratie")]
    public string Registratie { get; set; }
}
