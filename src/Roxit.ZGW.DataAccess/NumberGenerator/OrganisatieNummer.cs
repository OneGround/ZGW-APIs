using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.DataAccess.NumberGenerator;

[Table("organisatie_nummers")]
public class OrganisatieNummer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("creationtime")]
    public DateTime CreationTime { get; set; }

    [Column("modificationtime")]
    public DateTime ModificationTime { get; set; }

    [Required]
    [MaxLength(9)]
    [Column("rsin")]
    public string Rsin { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("entiteit")]
    public string EntiteitNaam { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("formaat")]
    public string Formaat { get; set; }

    [Required]
    [Column("huidig_nummer")]
    public long HuidigNummer { get; set; }

    [Column("huidig_nummer_entiteit")]
    [MaxLength(50)]
    public string HuidigNummerEntiteit { get; set; }
}
