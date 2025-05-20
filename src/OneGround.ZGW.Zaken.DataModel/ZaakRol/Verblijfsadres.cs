using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakRol;

[Table("verblijfsadresen")]
public class Verblijfsadres : IBaseEntity
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

    [Required]
    [Column("aoahuisnummer")]
    public int AoaHuisnummer { get; set; }

    [Column("aoahuisletter")]
    [MaxLength(1)]
    public string AoaHuisletter { get; set; }

    [Column("aoahuisnummertoevoeging")]
    [MaxLength(4)]
    public string AoaHuisnummertoevoeging { get; set; }

    [Column("inplocatiebeschrijving")]
    [MaxLength(1000)]
    public string InpLocatiebeschrijving { get; set; }
}
