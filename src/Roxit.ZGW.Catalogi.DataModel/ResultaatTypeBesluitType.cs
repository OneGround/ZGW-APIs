using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("resultaattypebesluittypen")]
public class ResultaatTypeBesluitType : OwnedEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("resultaattype_id")]
    public Guid ResultaatTypeId { get; set; }

    [ForeignKey(nameof(ResultaatTypeId))]
    public ResultaatType ResultaatType { get; set; }

    [Column("besluittype_omschrijving")]
    [MaxLength(80)]
    public string BesluitTypeOmschrijving { get; set; }

    // Note: Not mapped field because this is a soft relation between resultaattype and besluittype
    //   (soft relation is mapped: besluittype.omschrijving <-> resultaattypebesluittype(v1_3).besluittype_omschrijving within geldigheid)
    [NotMapped]
    public BesluitType BesluitType { get; set; }
}
