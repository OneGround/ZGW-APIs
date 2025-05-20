using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("besluittypeinformatieobjecttypen")]
public class BesluitTypeInformatieObjectType : OwnedEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("besluittype_id")]
    public Guid BesluitTypeId { get; set; }

    [ForeignKey(nameof(BesluitTypeId))]
    public BesluitType BesluitType { get; set; }

    [Column("informatieobjecttype_omschrijving")]
    [MaxLength(100)]
    public string InformatieObjectTypeOmschrijving { get; set; }

    // Note: Not mapped field because this is a soft relation between informatieobjecttype and besluittype
    //   (soft relation is mapped: besluittype.omschrijving <-> besluittypeinformatieobjecttype(v1_3).informatieobjecttype_omschrijving within geldigheid)
    [NotMapped]
    public InformatieObjectType InformatieObjectType { get; set; }
}
