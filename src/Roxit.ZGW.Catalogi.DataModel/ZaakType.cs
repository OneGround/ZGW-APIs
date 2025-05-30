using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.DataModel;

[Table("zaaktypen")]
public class ZaakType : OwnedEntity, IAuditableEntity, IConceptEntity, IValidityEntity, ICatalogusEntity
{
    [NotMapped]
    public string Url => $"/zaaktypen/{Id}";

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

    [MaxLength(50)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [MaxLength(80)]
    [Column("omschrijving")]
    public string Omschrijving { get; set; }

    [MaxLength(80)]
    [Column("omschrijvinggeneriek")]
    public string OmschrijvingGeneriek { get; set; }

    [Column("vertrouwelijkheidaanduiding", TypeName = "smallint")]
    public VertrouwelijkheidAanduiding VertrouwelijkheidAanduiding { get; set; }

    [Required]
    [Column("doel")]
    public string Doel { get; set; }

    [Required]
    [Column("aanleiding")]
    public string Aanleiding { get; set; }

    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Column("indicatieinternofextern", TypeName = "smallint")]
    public IndicatieInternOfExtern IndicatieInternOfExtern { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("handelinginitiator")]
    public string HandelingInitiator { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("onderwerp")]
    public string Onderwerp { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("handelingbehandelaar")]
    public string HandelingBehandelaar { get; set; }

    [Required]
    [Column("doorlooptijd")]
    public Period Doorlooptijd { get; set; }

    [Column("servicenorm")]
    public Period Servicenorm { get; set; }

    [Column("opschortingenaanhoudingmogelijk")]
    public bool OpschortingEnAanhoudingMogelijk { get; set; }

    [Column("verlengingmogelijk")]
    public bool VerlengingMogelijk { get; set; }

    [Column("verlengingstermijn")]
    public Period VerlengingsTermijn { get; set; }

    [Column("trefwoorden")]
    public string[] Trefwoorden { get; set; }

    [Column("publicatieindicatie")]
    public bool PublicatieIndicatie { get; set; }

    [Column("publicatietekst")]
    public string PublicatieTekst { get; set; }

    [Column("verantwoordingsrelatie")]
    public string[] Verantwoordingsrelatie { get; set; }

    [Required]
    [Column("productenofdiensten")]
    public string[] ProductenOfDiensten { get; set; }

    [Column("selectielijstprocestype")]
    public string SelectielijstProcestype { get; set; }

    [Required]
    public ReferentieProces ReferentieProces { get; set; }

    [Column("catalogus_id")]
    public Guid CatalogusId { get; set; }

    [ForeignKey(nameof(CatalogusId))]
    public Catalogus Catalogus { get; set; }

    [Column("begingeldigheid")]
    public DateOnly BeginGeldigheid { get; set; }

    [Column("eindegeldigheid")]
    public DateOnly? EindeGeldigheid { get; set; }

    [Column("versiedatum")]
    public DateOnly VersieDatum { get; set; }

    [Column("concept")]
    public bool Concept { get; set; } = true;

    [MaxLength(50)]
    [Column("verantwoordelijke")]
    public string Verantwoordelijke { get; set; }

    // Note: De CATALOGUS waaraan het ZAAKTYPE is ontleend.
    [Column("broncatalogus", TypeName = "jsonb")]
    public BronCatalogus BronCatalogus { get; set; }

    // Note: Het zaaktype binnen de CATALOGUS waaraan dit ZAAKTYPE is ontleend.
    [Column("bronzaaktype", TypeName = "jsonb")]
    public BronZaaktype BronZaaktype { get; set; }

    [Column("beginObject")]
    public DateOnly? BeginObject { get; set; }

    [Column("eindeObject")]
    public DateOnly? EindeObject { get; set; }

    public List<ZaakObjectType> ZaakObjectTypen { get; set; }

    public List<StatusType> StatusTypen { get; set; }

    public List<RolType> RolTypen { get; set; }

    public List<Eigenschap> Eigenschappen { get; set; }

    public List<ResultaatType> ResultaatTypen { get; set; }

    public List<ZaakTypeDeelZaakType> ZaakTypeDeelZaakTypen { get; set; }

    public List<ZaakTypeGerelateerdeZaakType> ZaakTypeGerelateerdeZaakTypen { get; set; }

    public List<ZaakTypeBesluitType> ZaakTypeBesluitTypen { get; set; }

    public List<ZaakTypeInformatieObjectType> ZaakTypeInformatieObjectTypen { get; set; }
}
