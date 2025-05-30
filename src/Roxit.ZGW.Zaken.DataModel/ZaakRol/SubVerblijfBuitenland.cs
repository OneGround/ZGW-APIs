using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakRol;

[Table("subverblijfbuitenland")]
public class SubVerblijfBuitenland : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(4)]
    [Column("lndlandcode")]
    public string LndLandcode { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("lndlandnaam")]
    public string LndLandnaam { get; set; }

    [MaxLength(35)]
    [Column("subadresbuitenland1")]
    public string SubAdresBuitenland1 { get; set; }

    [MaxLength(35)]
    [Column("subadresbuitenland2")]
    public string SubAdresBuitenland2 { get; set; }

    [MaxLength(35)]
    [Column("subadresbuitenland3")]
    public string SubAdresBuitenland3 { get; set; }
}
