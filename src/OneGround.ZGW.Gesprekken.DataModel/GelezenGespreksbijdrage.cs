using System;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

/// <summary>
/// Registratie dat een gespreksdeelname een gespreksbijdrage heeft gelezen (relatieklasse GELEZEN GESPREKSBIJDRAGE).
/// </summary>
[Table("gelezengespreksbijdragen")]
public class GelezenGespreksbijdrage : AuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/gelezengespreksbijdragen/{Id}";

    [Column("gespreksbijdrage_id")]
    public Guid GespreksbijdrageId { get; set; }

    public Gespreksbijdrage Gespreksbijdrage { get; set; }

    [Column("gespreksdeelname_id")]
    public Guid GespreksdeelnameId { get; set; }

    [Column("momentgelezen")]
    public DateTime MomentGelezen { get; set; }
}
