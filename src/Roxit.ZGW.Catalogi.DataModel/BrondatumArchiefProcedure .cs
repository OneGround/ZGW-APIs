using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("brondatumarchiefproceduren")]
public class BronDatumArchiefProcedure : IBaseEntity
{
    [Key, ForeignKey(nameof(ResultaatType))]
    [Column("resulttype_id")]
    public Guid Id { get; set; }

    public ResultaatType ResultaatType { get; set; }

    [Required]
    [Column("afleidingswijze")]
    public Afleidingswijze Afleidingswijze { get; set; }

    [Column("datumkenmerk")]
    [MaxLength(80)]
    public string DatumKenmerk { get; set; }

    [Column("einddatumbekend")]
    public bool EindDatumBekend { get; set; }

    [Column("objecttype")]
    public ObjectType? ObjectType { get; set; }

    [Column("registratie")]
    [MaxLength(80)]
    public string Registratie { get; set; }

    [Column("procestermijn")]
    public Period ProcesTermijn { get; set; }
}
