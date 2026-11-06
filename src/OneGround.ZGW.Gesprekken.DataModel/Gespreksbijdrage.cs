using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

/// <summary>
/// Wat een deelnemer toevoegt aan het gesprek. Een bijdrage wordt nooit gewijzigd; alleen beheer kan haar redigeren
/// (verwijderen, ook uit de eventhistorie).
/// </summary>
[Table("gespreksbijdragen")]
public class Gespreksbijdrage : AuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/gespreksbijdragen/{Id}";

    [Column("gesprek_id")]
    public Guid GesprekId { get; set; }

    public Gesprek Gesprek { get; set; }

    [Column("verzender_id")]
    public Guid VerzenderId { get; set; }

    public Gespreksdeelname Verzender { get; set; }

    [Column("isreactieop_id")]
    public Guid? IsReactieOpId { get; set; }

    [Required]
    [MaxLength(5000)]
    [Column("tekstgespreksbijdrage")]
    public string TekstGespreksBijdrage { get; set; }

    [Column("automatisch")]
    public bool Automatisch { get; set; }

    [Column("leveringsmoment")]
    public DateTime Leveringsmoment { get; set; }

    public List<Bijlage> Bijlagen { get; set; } = [];
}
