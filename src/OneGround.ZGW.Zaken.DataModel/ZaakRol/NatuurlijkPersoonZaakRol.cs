using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakRol;

[Table("zaakrollen_natuurlijk_personen")]
public class NatuurlijkPersoonZaakRol : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [MaxLength(9)]
    [Column("inpbsn")]
    public string InpBsn { get; set; }

    [MaxLength(17)]
    [Column("anpidentificatie")]
    public string AnpIdentificatie { get; set; }

    [MaxLength(10)]
    [Column("inpanummer")]
    public string InpANummer { get; set; }

    [MaxLength(200)]
    [Column("geslachtsnaam")]
    public string Geslachtsnaam { get; set; }

    [MaxLength(80)]
    [Column("voorvoegselgeslachtsnaam")]
    public string VoorvoegselGeslachtsnaam { get; set; }

    [MaxLength(20)]
    [Column("voorletters")]
    public string Voorletters { get; set; }

    [MaxLength(200)]
    [Column("voornamen")]
    public string Voornamen { get; set; }

    [Column("geslachtsaanduiding", TypeName = "smallint")]
    public Geslachtsaanduiding? Geslachtsaanduiding { get; set; }

    [Column("geboortedatum", TypeName = "date")]
    public DateTime? Geboortedatum { get; set; }

    [Column("verblijfsadres_id")]
    public Guid? VerblijfsadresId { get; set; }

    [ForeignKey("VerblijfsadresId")]
    public Verblijfsadres Verblijfsadres { get; set; }

    [Column("subverblijfbuitenland_id")]
    public Guid? SubVerblijfBuitenlandId { get; set; }

    [ForeignKey("SubVerblijfBuitenlandId")]
    public SubVerblijfBuitenland SubVerblijfBuitenland { get; set; }

    [Column("zaakrol_id")]
    public Guid ZaakRolId { get; set; }

    [ForeignKey("ZaakRolId")]
    public ZaakRol ZaakRol { get; set; }
}
