using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneGround.ZGW.Gesprekken.DataModel;

/// <summary>
/// Een bestand als bijlage bij een gespreksbijdrage, met de weergavegegevens van dat bestand. Een gekoppeld bestand
/// verandert niet, dus de kopie kan niet verouderen.
/// </summary>
[Table("bijlagen")]
public class Bijlage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("gespreksbijdrage_id")]
    public Guid GespreksbijdrageId { get; set; }

    [Column("bestand_id")]
    public Guid BestandId { get; set; }

    [MaxLength(200)]
    [Column("titel")]
    public string Titel { get; set; }

    [MaxLength(255)]
    [Column("bestandsnaam")]
    public string Bestandsnaam { get; set; }

    [MaxLength(100)]
    [Column("contenttype")]
    public string ContentType { get; set; }

    [Column("bestandsomvang")]
    public long? Bestandsomvang { get; set; }
}
