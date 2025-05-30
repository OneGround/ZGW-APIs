using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("resultaattypen")]
public class ResultaatType : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/resultaattypen/{Id}";

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
    [Column("zaaktype_id")]
    public Guid ZaakTypeId { get; set; }

    [ForeignKey("ZaakTypeId")]
    public virtual ZaakType ZaakType { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("omschrijving")]
    public string Omschrijving { get; set; }

    [Required]
    [Column("resultaattypeomschrijving")]
    [MaxLength(1000)]
    public string ResultaatTypeOmschrijving { get; set; }

    [Required]
    [Column("omschrijvinggeneriek")]
    public string OmschrijvingGeneriek { get; set; }

    [Required]
    [Column("selectielijstklasse")]
    [MaxLength(1000)]
    public string SelectieLijstKlasse { get; set; }

    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("archiefnominatie", TypeName = "smallint")]
    public ArchiefNominatie? ArchiefNominatie { get; set; }

    [Column("archiefactietermijn")]
    public Period ArchiefActieTermijn { get; set; }

    public BronDatumArchiefProcedure BronDatumArchiefProcedure { get; set; }

    [Column("procesobjectaard")]
    [MaxLength(200)]
    public string ProcesObjectAard { get; set; }

    [Column("begingeldigheid")]
    public DateOnly? BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }

    [Column("indicatieSpecifiek")]
    public bool? IndicatieSpecifiek { get; set; }

    [Column("procestermijn")]
    public Period ProcesTermijn { get; set; }

    public List<ResultaatTypeBesluitType> ResultaatTypeBesluitTypen { get; set; }

    // TODO: Not clear how to model the informatieobjecttypen (we asked VNG for). For now we don't store anything
    // ----

    // TODO: We ask VNG how the relations can be edited:
    //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501

    //[Column("zaakobjecttype_id")]
    //public Guid? ZaakObjectTypeId { get; set; }

    //[ForeignKey("ZaakObjectTypeId")]
    //public virtual ZaakObjectType ZaakObjectType { get; set; }
    // ----
}
