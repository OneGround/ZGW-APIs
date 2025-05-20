using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("besluittypen")]
public class BesluitType : OwnedEntity, IAuditableEntity, IConceptEntity, IValidityEntity, ICatalogusEntity
{
    [NotMapped]
    public string Url => $"/besluittypen/{Id}";

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

    [Column("catalogus_id")]
    public Guid CatalogusId { get; set; }

    [ForeignKey(nameof(CatalogusId))]
    public Catalogus Catalogus { get; set; }

    [NotMapped] // Not mapped from DB anymore due soft relations >= v1.3 of ZTC [Zt->Bt'n and Bt->Zt'n]
    public List<BesluitTypeZaakType> BesluitTypeZaakTypen { get; set; }

    [NotMapped] // Not mapped from DB anymore due soft relations >= v1.3 of ZTC [Rt->Bt'n and Bt->Rt'n]
    public List<ResultaatTypeBesluitType> BesluitTypeResultaatTypen { get; set; } = [];

    [Column("omschrijving")]
    [MaxLength(80)]
    public string Omschrijving { get; set; }

    [Column("omschrijvinggeneriek")]
    [MaxLength(80)]
    public string OmschrijvingGeneriek { get; set; }

    [Column("besluitcategorie")]
    [MaxLength(40)]
    public string BesluitCategorie { get; set; }

    [Column("reactietermijn")]
    public Period ReactieTermijn { get; set; }

    [Column("publicatieindicatie")]
    public bool PublicatieIndicatie { get; set; }

    [Column("publicatietekst")]
    public string PublicatieTekst { get; set; }

    [Column("publicatietermijn")]
    public Period PublicatieTermijn { get; set; }

    [Column("toelichting")]
    public string Toelichting { get; set; }

    public List<BesluitTypeInformatieObjectType> BesluitTypeInformatieObjectTypen { get; set; }

    [Column("begingeldigheid")]
    public DateOnly BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }

    [Column("concept")]
    public bool Concept { get; set; } = true;

    // TODO: Maps:
    /*
       "vastgelegd_in": []  Array of strings (informatieobjecttypen) unique Omschrijving van de aard van informatieobjecten van dit INFORMATIEOBJECTTYPE
    */
}
