using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Besluiten.DataModel;

[Table("besluiten")]
public class Besluit : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/besluiten/{Id}";

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
    [MaxLength(50)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [Required]
    [MaxLength(9)]
    [Column("verantwoordelijkeorganisatie")]
    public string VerantwoordelijkeOrganisatie { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("besluittype")]
    public string BesluitType { get; set; }

    [MaxLength(200)]
    [Column("zaak")]
    public string Zaak { get; set; }

    [Column("zaakbesluit")]
    public string ZaakBesluitUrl { get; set; }

    [Required]
    [Column("datum")]
    public DateOnly Datum { get; set; }

    [Column("toelichting")]
    public string Toelichting { get; set; }

    [MaxLength(50)]
    [Column("bestuursorgaan")]
    public string BestuursOrgaan { get; set; }

    [Column("ingangsdatum")]
    public DateOnly IngangsDatum { get; set; }

    [Column("vervaldatum")]
    public DateOnly? VervalDatum { get; set; }

    [Column("vervalreden")]
    public VervalReden? VervalReden { get; set; }

    [Column("publicatiedatum")]
    public DateOnly? PublicatieDatum { get; set; }

    [Column("verzenddatum")]
    public DateOnly? VerzendDatum { get; set; }

    [Column("uiterlijkeReactiedatum")]
    public DateOnly? UiterlijkeReactieDatum { get; set; }

    public List<BesluitInformatieObject> BesluitInformatieObjecten { get; set; }
}
