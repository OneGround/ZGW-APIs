using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakRol;

[Table("zaakrollen")]
public class ZaakRol : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/rollen/{Id}";

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

    [Column("zaak_id")]
    public Guid ZaakId { get; set; }

    [ForeignKey("ZaakId")]
    public Zaak Zaak { get; set; }

    [Column("betrokkene")]
    [MaxLength(1000)]
    public string Betrokkene { get; set; }

    [Required]
    [Column("betrokkenetype", TypeName = "smallint")]
    public BetrokkeneType BetrokkeneType { get; set; }

    [Column("afwijkendeNaamBetrokkene")]
    [MaxLength(625)]
    public string AfwijkendeNaamBetrokkene { get; set; }

    [Column("roltype")]
    [MaxLength(1000)]
    public string RolType { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("roltoelichting")]
    public string Roltoelichting { get; set; }

    [Column("registratiedatum")]
    public DateTime Registratiedatum { get; set; }

    [Required]
    [Column("omschrijving")]
    [MaxLength(100)]
    public string Omschrijving { get; set; }

    [Required]
    [Column("omschrijvinggeneriek", TypeName = "smallint")]
    public OmschrijvingGeneriek OmschrijvingGeneriek { get; set; }

    [Column("indicatiemachtiging", TypeName = "smallint")]
    public IndicatieMachtiging? IndicatieMachtiging { get; set; }

    [Column("contactpersoonrol_id")]
    public Guid? ContactpersoonRolId { get; set; }

    [ForeignKey("ContactpersoonRolId")]
    public ContactpersoonRol ContactpersoonRol { get; set; }

    public virtual NatuurlijkPersoonZaakRol NatuurlijkPersoon { get; set; }

    public virtual NietNatuurlijkPersoonZaakRol NietNatuurlijkPersoon { get; set; }

    public virtual VestigingZaakRol Vestiging { get; set; }

    public virtual MedewerkerZaakRol Medewerker { get; set; }

    public virtual OrganisatorischeEenheidZaakRol OrganisatorischeEenheid { get; set; }
}
