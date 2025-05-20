using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Notificaties.DataModel;

[Table("kanalen")]
public class Kanaal : IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/kanaal/{Id}";

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
    [MaxLength(50)]
    [Column("naam")]
    public string Naam { get; set; }

    [MaxLength(200)]
    [Column("documentatielink")]
    public string DocumentatieLink { get; set; }

    [Required]
    [Column("filters")]
    public string[] Filters { get; set; }

    public IList<AbonnementKanaal> AbonnementKanalen { get; set; }
}
