using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten")]
public class ZaakObject : OwnedEntity, IAuditableEntity, IUrlEntity, IZaakEntity
{
    [NotMapped]
    public string Url => $"/zaakobjecten/{Id}";

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

    [MaxLength(1000)]
    [Column("object")]
    public string Object { get; set; }

    [MaxLength(1000)]
    [Column("zaakobjecttype")]
    public string ZaakObjectType { get; set; }

    [Required]
    [Column("objecttype", TypeName = "smallint")]
    public ObjectType ObjectType { get; set; }

    [MaxLength(100)]
    [Column("objecttypeoverige")]
    public string ObjectTypeOverige { get; set; }

    public ObjectTypeOverigeDefinitie ObjectTypeOverigeDefinitie { get; set; }

    [MaxLength(80)]
    [Column("relatieomschrijving")]
    public string RelatieOmschrijving { get; set; }

    public AdresZaakObject Adres { get; set; }

    public BuurtZaakObject Buurt { get; set; }

    public PandZaakObject Pand { get; set; }

    public KadastraleOnroerendeZaakObject KadastraleOnroerendeZaak { get; set; }

    public GemeenteZaakObject Gemeente { get; set; }

    public TerreinGebouwdObjectZaakObject TerreinGebouwdObject { get; set; }

    public OverigeZaakObject Overige { get; set; }

    public WozWaardeZaakObject WozWaardeObject { get; set; }
}
