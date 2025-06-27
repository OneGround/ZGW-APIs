using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

[Table("zaken")]
public class Zaak : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => $"/zaken/{Id}";

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

    [MaxLength(40)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [Required]
    [MaxLength(9)]
    [Column("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [MaxLength(80)]
    [Column("omschrijving")]
    public string Omschrijving { get; set; }

    [Column("toelichting")]
    public string Toelichting { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("zaaktype")]
    public string Zaaktype { get; set; }

    [Column("registratiedatum")]
    public DateOnly? Registratiedatum { get; set; }

    [Required]
    [MaxLength(9)]
    [Column("verantwoordelijkeorganisatie")]
    public string VerantwoordelijkeOrganisatie { get; set; }

    [Column("einddatum")]
    public DateOnly? Einddatum { get; set; }

    [Column("einddatumgepland")]
    public DateOnly? EinddatumGepland { get; set; }

    [Column("uiterlijkeeinddatumafdoening")]
    public DateOnly? UiterlijkeEinddatumAfdoening { get; set; }

    [Column("publicatiedatum")]
    public DateOnly? Publicatiedatum { get; set; }

    [Required]
    [Column("communicatiekanaal")]
    [MaxLength(1000)]
    public string Communicatiekanaal { get; set; }

    [Column("betalingsindicatieweergave")]
    [MaxLength(50)]
    public string BetalingsindicatieWeergave { get; set; }

    [Column("laatstebetaaldatum")]
    public DateTime? LaatsteBetaaldatum { get; set; }

    public ZaakVerlenging Verlenging { get; set; }

    public ZaakOpschorting Opschorting { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("selectielijstklasse")]
    public string Selectielijstklasse { get; set; }

    [Column("hoofdzaak_id")]
    public Guid? HoofdzaakId { get; set; }

    [ForeignKey("HoofdzaakId")]
    public Zaak Hoofdzaak { get; set; }

    [MaxLength(50)]
    [Column("archiefnominatie", TypeName = "smallint")]
    public ArchiefNominatie? Archiefnominatie { get; set; }

    [Column("archiefactiedatum")]
    public DateOnly? Archiefactiedatum { get; set; }

    public ZaakResultaat Resultaat { get; set; }

    [Column("archiefstatus", TypeName = "smallint")]
    public ArchiefStatus Archiefstatus { get; set; }

    [Required]
    [Column("startdatum")]
    public DateOnly Startdatum { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column("betalingsindicatie", TypeName = "smallint")]
    public BetalingsIndicatie BetalingsIndicatie { get; set; }

    [Column("vertrouwelijkheidaanduiding", TypeName = "smallint")]
    public VertrouwelijkheidAanduiding VertrouwelijkheidAanduiding { get; set; }

    [Column("productenofdiensten")]
    public List<string> ProductenOfDiensten { get; set; } = [];

    [Column("zaakgeometrie")]
    public Geometry Zaakgeometrie { get; set; }

    public List<Zaak> Deelzaken { get; set; }

    public List<RelevanteAndereZaak> RelevanteAndereZaken { get; set; }

    public List<ZaakKenmerk> Kenmerken { get; set; }

    public List<ZaakEigenschap> ZaakEigenschappen { get; set; }

    public List<ZaakStatus> ZaakStatussen { get; set; }

    public List<ZaakBesluit> ZaakBesluiten { get; set; }

    public List<ZaakObject.ZaakObject> ZaakObjecten { get; set; }

    public List<ZaakInformatieObject> ZaakInformatieObjecten { get; set; }

    public List<ZaakRol.ZaakRol> ZaakRollen { get; set; }

    public List<KlantContact> KlantContacten { get; set; }

    [MaxLength(9)]
    [Column("opdrachtgevendeorganisatie")]
    public string OpdrachtgevendeOrganisatie { get; set; }

    [MaxLength(200)]
    [Column("processobjectaard")]
    public string Processobjectaard { get; set; }

    [Column("startdatumbewaartermijn")]
    public DateOnly? StartdatumBewaartermijn { get; set; }

    public ZaakProcessobject Processobject { get; set; }

    [Column("catalogus_id")]
    public Guid CatalogusId { get; set; }
}
