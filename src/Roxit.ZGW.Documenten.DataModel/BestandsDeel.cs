using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Documenten.DataModel;

[Table("bestandsdelen")]
public class BestandsDeel : IBaseEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/bestandsdelen/{Id}";

    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("volgnummer")]
    public int Volgnummer { get; set; }

    [Column("omvang")]
    public int Omvang { get; set; }

    [Column("voltooid")]
    public bool Voltooid { get; set; }

    [Column("enkelvoudiginformatieobjectversie_id")]
    public Guid EnkelvoudigInformatieObjectVersieId { get; set; }

    [ForeignKey(nameof(EnkelvoudigInformatieObjectVersieId))]
    public EnkelvoudigInformatieObjectVersie EnkelvoudigInformatieObjectVersie { get; set; }

    [MaxLength(250)]
    [Column("uploadpart_id")]
    public string UploadPartId { get; set; }
}
