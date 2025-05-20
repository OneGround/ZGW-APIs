using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Autorisaties.DataModel;

[Table("autorisaties")]
public class Autorisatie : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("component", TypeName = "smallint")]
    public Component Component { get; set; }

    [Column("max_vertrouwelijkheidaanduiding", TypeName = "smallint")]
    public VertrouwelijkheidAanduiding? MaxVertrouwelijkheidaanduiding { get; set; }

    [Required]
    [Column("scopes")]
    public string[] Scopes { get; set; }

    [Column("applicatie_id")]
    public Guid ApplicatieId { get; set; }

    [ForeignKey(nameof(ApplicatieId))]
    public Applicatie Applicatie { get; set; }

    [MaxLength(1000)]
    [Column("zaak_type")]
    public string ZaakType { get; set; }

    [MaxLength(1000)]
    [Column("besluit_type")]
    public string BesluitType { get; set; }

    [MaxLength(1000)]
    [Column("informatie_object_type")]
    public string InformatieObjectType { get; set; }
}
