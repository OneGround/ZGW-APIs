using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("catalogussen")]
public class Catalogus : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/catalogussen/{Id}";

    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("creationtime")]
    public DateTime CreationTime { get; set; }

    [Column("modificationtime")]
    public DateTime? ModificationTime { get; set; }

    [MaxLength(50)]
    [Column("createdby")]
    public string CreatedBy { get; set; }

    [MaxLength(50)]
    [Column("modifiedby")]
    public string ModifiedBy { get; set; }

    [Required]
    [MaxLength(5)]
    [Column("domein")]
    public string Domein { get; set; }

    [Required]
    [MaxLength(9)]
    [Column("rsin")]
    public string Rsin { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("contactpersoonbeheernaam")]
    public string ContactpersoonBeheerNaam { get; set; }

    [MaxLength(20)]
    [Column("contactpersoonbeheertelefoonnummer")]
    public string ContactpersoonBeheerTelefoonnummer { get; set; }

    [MaxLength(254)]
    [Column("contactpersoonbeheeremailadres")]
    public string ContactpersoonBeheerEmailadres { get; set; }

    [MaxLength(200)]
    [Column("naam")]
    public string Naam { get; set; }

    [MaxLength(20)]
    [Column("versie")]
    public string Versie { get; set; }

    [Column("begindatumVersie")]
    public DateOnly? BegindatumVersie { get; set; }

    public List<ZaakType> ZaakTypes { get; set; }

    public List<BesluitType> BesluitTypes { get; set; }

    public List<InformatieObjectType> InformatieObjectTypes { get; set; }
}
