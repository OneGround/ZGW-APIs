using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.DataModel;

[Table("enkelvoudiginformatieobjecten")]
public class EnkelvoudigInformatieObject : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/enkelvoudiginformatieobjecten/{Id}";

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
    [MaxLength(200)]
    [Column("informatieobjecttype")]
    public string InformatieObjectType { get; set; }

    [Column("indicatiegebruiksrecht")]
    public bool? IndicatieGebruiksrecht { get; set; }

    [Column("locked")]
    public bool Locked { get; set; }

    [Column("lock")]
    public string Lock { get; set; }

    [Column("latest_enkelvoudiginformatieobjectversie_id")]
    public Guid? LatestEnkelvoudigInformatieObjectVersieId { get; set; }

    public EnkelvoudigInformatieObjectVersie LatestEnkelvoudigInformatieObjectVersie { get; set; }

    public List<ObjectInformatieObject> ObjectInformatieObjecten { get; set; } = [];

    public List<EnkelvoudigInformatieObjectVersie> EnkelvoudigInformatieObjectVersies { get; set; } = [];

    public List<GebruiksRecht> GebruiksRechten { get; set; } = [];

    public List<Verzending> Verzendingen { get; set; } = [];

    [Column("catalogus_id")]
    public Guid CatalogusId { get; set; }

    // Note: maps with the Postgres build in xmin concurrency token (uint is the C# equivalent for the Postgres 'xid' type)
    public uint RowVersion { get; set; }
}
