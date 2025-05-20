using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("informatieobjecttypen")]
public class InformatieObjectType : OwnedEntity, IAuditableEntity, IConceptEntity, IValidityEntity, ICatalogusEntity
{
    [NotMapped]
    public string Url => $"/informatieobjecttypen/{Id}";

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
    [MaxLength(80)]
    [Column("omschrijving")]
    public string Omschrijving { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("vertrouwelijkheidaanduiding")]
    public VertrouwelijkheidAanduiding VertrouwelijkheidAanduiding { get; set; }

    [Required]
    [Column("concept")]
    public bool Concept { get; set; }

    [Required]
    [Column("begingeldigheid")]
    public DateOnly BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }

    [Column("informatieobjectcategorie")]
    public string InformatieObjectCategorie { get; set; }

    [Column("trefwoord")]
    public string[] Trefwoord { get; set; }

    [Column("omschrijvinggeneriek", TypeName = "jsonb")]
    public OmschrijvingGeneriek OmschrijvingGeneriek { get; set; }

    [Column("catalogus_id")]
    public Guid CatalogusId { get; set; }

    public Catalogus Catalogus { get; set; }

    [NotMapped]
    public List<BesluitTypeInformatieObjectType> InformatieObjectTypeBesluitTypen { get; set; }

    [NotMapped]
    public List<ZaakTypeInformatieObjectType> InformatieObjectTypeZaakTypen { get; set; }
}
