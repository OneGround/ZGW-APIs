using System;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("statustype_verplichte_eigenschappen")]
public class StatusTypeVerplichteEigenschap : OwnedEntity
{
    [Column("eigenschap_id")]
    public Guid EigenschapId { get; set; }

    [ForeignKey(nameof(EigenschapId))]
    public Eigenschap Eigenschap { get; set; }

    [Column("status_id")]
    public Guid StatusTypeId { get; set; }

    [ForeignKey(nameof(StatusTypeId))]
    public StatusType StatusType { get; set; }
}
