using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Autorisaties.DataModel;

[Table("applicaties")]
public class Applicatie : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/applicaties/{Id}";

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
    [MaxLength(100)]
    [Column("label")]
    public string Label { get; set; }

    [Required]
    [Column("heeft_alle_autorisaties")]
    public bool HeeftAlleAutorisaties { get; set; }

    public List<ApplicatieClient> ClientIds { get; set; }

    public List<Autorisatie> Autorisaties { get; set; }

    public List<FutureAutorisatie> FutureAutorisaties { get; set; }
}
