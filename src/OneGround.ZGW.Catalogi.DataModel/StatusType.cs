using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

[Table("statustypen")]
public class StatusType : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/statustypen/{Id}";

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

    [MaxLength(80)]
    [Column("omschrijvinggeneriek")]
    public string OmschrijvingGeneriek { get; set; }

    [MaxLength(1000)]
    [Column("statustekst")]
    public string StatusTekst { get; set; }

    [Required]
    [Column("volgnummer")]
    public int VolgNummer { get; set; }

    [NotMapped]
    public bool IsEindStatus { get; set; }

    [Column("informeren")]
    public bool Informeren { get; set; }

    [Column("doorlooptijd")]
    public Period Doorlooptijd { get; set; }

    [MaxLength(1000)]
    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("checklistitemStatustype", TypeName = "jsonb")]
    public CheckListItemStatusType[] CheckListItemStatustypes { get; set; }

    public List<StatusTypeVerplichteEigenschap> StatusTypeVerplichteEigenschappen { get; set; }

    [Column("begingeldigheid")]
    public DateOnly? BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }

    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey("ZaakTypeId")]
    public virtual ZaakType ZaakType { get; set; }

    // TODO: We ask VNG how the relations can be edited:
    //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501

    //[Column("zaakobjecttype_id")]
    //public Guid? ZaakObjectTypeId { get; set; }

    //[ForeignKey("ZaakObjectTypeId")]
    //public virtual ZaakObjectType ZaakObjectType { get; set; }
}
