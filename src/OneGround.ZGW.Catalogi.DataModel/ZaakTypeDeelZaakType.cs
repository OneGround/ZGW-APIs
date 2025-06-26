using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("zaaktypedeelzaaktypen")]
public class ZaakTypeDeelZaakType : OwnedEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey(nameof(ZaakTypeId))]
    public ZaakType ZaakType { get; set; }

    [Column("deelzaaktype_identificatie")]
    [MaxLength(80)]
    public string DeelZaakTypeIdentificatie { get; set; }

    // Note: Not mapped field because this is a soft relation between zaaktype and deelzaaktype
    //   (soft relation is mapped: zaaktype.identificatie <-> deelzaaktype(v1_3).deelzaaktype_identificatie within geldigheid)
    [NotMapped]
    public ZaakType DeelZaakType { get; set; }
}
