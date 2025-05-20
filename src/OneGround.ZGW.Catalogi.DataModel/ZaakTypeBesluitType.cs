using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("zaaktypebesluittypen")]
public class ZaakTypeBesluitType : OwnedEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey(nameof(ZaakTypeId))]
    public ZaakType ZaakType { get; set; }

    [Column("besluittype_omschrijving")]
    [MaxLength(80)]
    public string BesluitTypeOmschrijving { get; set; }

    // Note: Not mapped field because this is a soft relation between zaaktype and besluittype
    //   (soft relation is mapped: besluittype.omschrijving <-> besluittypezaaktype(v1_3).besluittype_omschrijving within geldigheid)
    [NotMapped]
    public BesluitType BesluitType { get; set; }
}
