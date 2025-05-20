using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Autorisaties.DataModel;

[Table("future_autorisaties")]
public class FutureAutorisatie : IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [StringLength(9, MinimumLength = 9)]
    [Column("owner")]
    public string Owner { get; set; }

    [Column("component", TypeName = "smallint")]
    public Component Component { get; set; }

    [Required]
    [Column("scopes")]
    public string[] Scopes { get; set; }

    [Column("max_vertrouwelijkheidaanduiding", TypeName = "smallint")]
    public VertrouwelijkheidAanduiding? MaxVertrouwelijkheidaanduiding { get; set; }

    [Required]
    [Column("applicatie_id")]
    public Guid ApplicatieId { get; set; }

    [ForeignKey(nameof(ApplicatieId))]
    public Applicatie Applicatie { get; set; }
}
