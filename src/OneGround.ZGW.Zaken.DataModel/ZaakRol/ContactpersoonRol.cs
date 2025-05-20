using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakRol;

[Table("contactpersoonrollen")]
public class ContactpersoonRol : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("emailadres")]
    [MaxLength(254)]
    public string EmailAdres { get; set; }

    [Column("functie")]
    [MaxLength(50)]
    public string Functie { get; set; }

    [Column("telefoonnummer")]
    [MaxLength(20)]
    public string Telefoonnummer { get; set; }

    [Required]
    [Column("naam")]
    [MaxLength(40)]
    public string Naam { get; set; }
}
