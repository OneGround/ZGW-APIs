using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Autorisaties.DataModel;

[Table("applicatie_clients")]
public class ApplicatieClient : IAuditableEntity
{
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
    [Column("client_id")]
    public string ClientId { get; set; }

    [Required]
    [Column("applicatie_id")]
    public Guid ApplicatieId { get; set; }

    [ForeignKey(nameof(ApplicatieId))]
    public Applicatie Applicatie { get; set; }
}
