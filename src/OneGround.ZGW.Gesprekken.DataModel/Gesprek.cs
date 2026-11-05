using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

/// <summary>
/// Een dialoog, met een aanleiding, tussen een klant van een gemeente, de gemeente en eventuele andere betrokkenen.
/// </summary>
[Table("gesprekken")]
public class Gesprek : AuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/gesprekken/{Id}";

    [Required]
    [MaxLength(50)]
    [Column("referentie")]
    public string Referentie { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("gespreksonderwerp")]
    public string Gespreksonderwerp { get; set; }

    [MaxLength(1000)]
    [Column("aanleidingzaak")]
    public string AanleidingZaak { get; set; }

    [MaxLength(1000)]
    [Column("aanleidingthema")]
    public string AanleidingThema { get; set; }

    [MaxLength(1000)]
    [Column("aanleidingproductdienst")]
    public string AanleidingProductDienst { get; set; }

    [MaxLength(1000)]
    [Column("aanleidingagendaafspraak")]
    public string AanleidingAgendaAfspraak { get; set; }

    [MaxLength(1000)]
    [Column("aanleidingplan")]
    public string AanleidingPlan { get; set; }

    [MaxLength(1000)]
    [Column("contactmoment")]
    public string Contactmoment { get; set; }

    [Column("aanvangsmomentgesprek")]
    public DateTime AanvangsmomentGesprek { get; set; }

    [Column("momenteindegesprek")]
    public DateTime? MomentEindeGesprek { get; set; }

    public List<Gespreksdeelname> Deelnames { get; set; } = [];

    public List<Gespreksbijdrage> Bijdragen { get; set; } = [];
}
