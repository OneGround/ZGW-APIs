using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("eigenschappen_specificaties")]
public class EigenschapSpecificatie : OwnedEntity, IBaseEntity
{
    [Key, ForeignKey(nameof(Eigenschap))]
    [Column("eigenschap_id")]
    public Guid Id { get; set; }

    public Eigenschap Eigenschap { get; set; }

    [MaxLength(32)]
    [Column("groep")]
    public string Groep { get; set; }

    [Column("formaat")]
    public Formaat Formaat { get; set; }

    [Required]
    [MaxLength(14)]
    [Column("lengte")]
    public string Lengte { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("kardinaliteit")]
    public string Kardinaliteit { get; set; }

    [Column("waardenverzameling")]
    public List<string> Waardenverzameling { get; set; }
}
