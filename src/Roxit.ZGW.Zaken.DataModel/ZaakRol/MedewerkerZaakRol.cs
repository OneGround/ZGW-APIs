using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakRol;

[Table("zaakrollen_medewerkers")]
public class MedewerkerZaakRol : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [MaxLength(24)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [MaxLength(200)]
    [Column("achternaam")]
    public string Achternaam { get; set; }

    [MaxLength(20)]
    [Column("voorletters")]
    public string Voorletters { get; set; }

    [MaxLength(10)]
    [Column("voorvoegselachternaam")]
    public string VoorvoegselAchternaam { get; set; }

    [Column("zaakrol_id")]
    public Guid ZaakRolId { get; set; }

    [ForeignKey("ZaakRolId")]
    public ZaakRol ZaakRol { get; set; }
}
