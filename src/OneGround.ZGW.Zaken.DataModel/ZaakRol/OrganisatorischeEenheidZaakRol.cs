using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakRol;

[Table("zaakrollen_organisatorische_eenheden")]
public class OrganisatorischeEenheidZaakRol : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [MaxLength(24)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [MaxLength(50)]
    [Column("naam")]
    public string Naam { get; set; }

    [MaxLength(24)]
    [Column("isgehuisvestin")]
    public string IsGehuisvestIn { get; set; }

    [Column("zaakrol_id")]
    public Guid ZaakRolId { get; set; }

    [ForeignKey("ZaakRolId")]
    public ZaakRol ZaakRol { get; set; }
}
