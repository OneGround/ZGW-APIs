using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakObject;

[Table("woz_object_aanduidingen")]
public class AanduidingWozObject : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("aoaidentificatie")]
    public string AoaIdentificatie { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("wplwoonplaatsnaam")]
    public string WplWoonplaatsNaam { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("goropenbareruimtenaam")]
    public string GorOpenbareRuimteNaam { get; set; }

    [MaxLength(7)]
    [Column("aoapostcode")]
    public string AoaPostcode { get; set; }

    [Column("aoahuisnummer")]
    public int AoaHuisnummer { get; set; }

    [MaxLength(1)]
    [Column("aoahuisletter")]
    public string AoaHuisletter { get; set; }

    [MaxLength(4)]
    [Column("aoahuisnummertoevoeging")]
    public string AoaHuisnummerToevoeging { get; set; }

    [MaxLength(1000)]
    [Column("locatieomschrijving")]
    public string LocatieOmschrijving { get; set; }
}
