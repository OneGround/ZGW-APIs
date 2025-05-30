using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("zaakobjecttypen")]
public class ZaakObjectType : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/zaakobjecttypen/{Id}";

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

    [Column("anderobjecttype")]
    public bool AnderObjectType { get; set; }

    [Column("begingeldigheid")]
    public DateOnly BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginobject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeobject")]
    public DateOnly? EindeObject { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("objecttype")]
    public string ObjectType { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("relatieomschrijving")]
    public string RelatieOmschrijving { get; set; }

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey("ZaakTypeId")]
    public virtual ZaakType ZaakType { get; set; }

    // TODO: We ask VNG how the relations can be edited:
    //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501

    //public List<ResultaatType> ResultaatTypen { get; set; }

    //public List<StatusType> StatusTypen { get; set; }
}
