using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

/// <summary>
/// Een deelname van een Partij of Actor aan een specifiek gesprek (relatieklasse GESPREKSDEELNAME).
/// </summary>
[Table("gespreksdeelnames")]
public class Gespreksdeelname : AuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/gespreksdeelnames/{Id}";

    [Column("gesprek_id")]
    public Guid GesprekId { get; set; }

    public Gesprek Gesprek { get; set; }

    [Column("type")]
    public DeelnameType Type { get; set; }

    [MaxLength(1000)]
    [Column("partij")]
    public string Partij { get; set; }

    [MaxLength(1000)]
    [Column("actor")]
    public string Actor { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("naam")]
    public string Naam { get; set; }

    [MaxLength(1000)]
    [Column("namens")]
    public string Namens { get; set; }

    [Column("aanvangsmomentdeelname")]
    public DateTime AanvangsmomentDeelname { get; set; }

    [Column("momenteindedeelname")]
    public DateTime? MomentEindeDeelname { get; set; }

    [NotMapped]
    public bool IsActief => MomentEindeDeelname == null;
}
