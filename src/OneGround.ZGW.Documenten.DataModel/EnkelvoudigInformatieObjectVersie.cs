using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.DataModel;

[Table("enkelvoudiginformatieobjectversies")]
public class EnkelvoudigInformatieObjectVersie : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url =>
        InformatieObject != null ? $"{InformatieObject.Url}/download?versie={Versie}"
        : LatestInformatieObject != null ? $"{LatestInformatieObject.Url}/download?versie={Versie}"
        : throw new NullReferenceException();

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

    [MaxLength(50)]
    [Column("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [Column("creatiedatum")]
    public DateOnly? CreatieDatum { get; set; }

    [MaxLength(200)]
    [Column("titel")]
    public string Titel { get; set; }

    [Column("vertrouwelijkheidaanduiding")]
    public VertrouwelijkheidAanduiding? Vertrouwelijkheidaanduiding { get; set; }

    [MaxLength(200)]
    [Column("auteur")]
    public string Auteur { get; set; }

    [Column("status")]
    public Status? Status { get; set; }

    [MaxLength(255)]
    [Column("formaat")]
    public string Formaat { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("taal")]
    public string Taal { get; set; }

    [Required]
    [Column("versie")]
    public int Versie { get; set; }

    [Required]
    [Column("beginregistratie")]
    public DateTime BeginRegistratie { get; set; }

    [MaxLength(255)]
    [Column("bestandsnaam")]
    public string Bestandsnaam { get; set; }

    [Column("inhoud")]
    public string Inhoud { get; set; }

    [Required]
    [Column("bestandsomvang")]
    public long Bestandsomvang { get; set; }

    [MaxLength(200)]
    [Column("link")]
    public string Link { get; set; }

    [MaxLength(1000)]
    [Column("beschrijving")]
    public string Beschrijving { get; set; }

    [Column("ontvangstdatum")]
    public DateOnly? OntvangstDatum { get; set; }

    [Column("verzenddatum")]
    public DateOnly? VerzendDatum { get; set; }

    [Column("integriteit_algoritme")]
    public Algoritme Integriteit_Algoritme { get; set; }

    [MaxLength(50)]
    [Column("ondertekening_soort")]
    public Soort? Ondertekening_Soort { get; set; }

    [Column("ondertekening_datum")]
    public DateOnly? Ondertekening_Datum { get; set; }

    [MaxLength(128)]
    [Column("integriteit_waarde")]
    public string Integriteit_Waarde { get; set; }

    [Column("integriteit_datum")]
    public DateOnly? Integriteit_Datum { get; set; }

    [Column("verschijningsvorm")]
    public string Verschijningsvorm { get; set; }

    [Column("trefwoorden")]
    public List<string> Trefwoorden { get; set; } = [];

    [Column("inhoud_is_vervallen")]
    public bool InhoudIsVervallen { get; set; }

    [Column("enkelvoudiginformatieobject_id")]
    public Guid EnkelvoudigInformatieObjectId { get; set; }

    public EnkelvoudigInformatieObject InformatieObject { get; set; }

    public EnkelvoudigInformatieObject LatestInformatieObject { get; set; }

    public List<BestandsDeel> BestandsDelen { get; set; } = [];

    [MaxLength(250)]
    [Column("multipartdocument_id")]
    public string MultiPartDocumentId { get; set; }
}

// ZZZ
[Table("enkelvoudiginformatieobject_locks_2")]
public class EnkelvoudigInformatieObjectLock2 : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    public string Url => "?";

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

    [Column("locked")]
    public bool Locked { get; set; }

    [Column("lock")]
    public string Lock { get; set; }

    public List<EnkelvoudigInformatieObject2> EnkelvoudigInformatieObjecten { get; set; } = [];

    // new
    public List<ObjectInformatieObject> ObjectInformatieObjecten { get; set; } = [];

    public List<GebruiksRecht> GebruiksRechten { get; set; } = [];

    public List<Verzending> Verzendingen { get; set; } = [];
}

// ZZZ
[Table("enkelvoudiginformatieobjecten_2")]
public class EnkelvoudigInformatieObject2 : OwnedEntity, IAuditableEntity, IUrlEntity
{
    [NotMapped]
    //public string Url => $"{Url}/download?versie={Versie}";
    public string Url => $"/enkelvoudiginformatieobjecten/{EnkelvoudigInformatieObjectLockId}";

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

    [MaxLength(50)]
    [Column("bronorganisatie")]
    public string Bronorganisatie { get; set; }

    [Column("creatiedatum")]
    public DateOnly? CreatieDatum { get; set; }

    [MaxLength(200)]
    [Column("titel")]
    public string Titel { get; set; }

    [Column("vertrouwelijkheidaanduiding")]
    public VertrouwelijkheidAanduiding? Vertrouwelijkheidaanduiding { get; set; }

    [MaxLength(200)]
    [Column("auteur")]
    public string Auteur { get; set; }

    [Column("status")]
    public Status? Status { get; set; }

    [MaxLength(255)]
    [Column("formaat")]
    public string Formaat { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("taal")]
    public string Taal { get; set; }

    [Required]
    [Column("versie")]
    public int Versie { get; set; }

    [Required]
    [Column("beginregistratie")]
    public DateTime BeginRegistratie { get; set; }

    [MaxLength(255)]
    [Column("bestandsnaam")]
    public string Bestandsnaam { get; set; }

    [Column("inhoud")]
    public string Inhoud { get; set; }

    [Required]
    [Column("bestandsomvang")]
    public long Bestandsomvang { get; set; }

    [MaxLength(200)]
    [Column("link")]
    public string Link { get; set; }

    [MaxLength(1000)]
    [Column("beschrijving")]
    public string Beschrijving { get; set; }

    [Column("ontvangstdatum")]
    public DateOnly? OntvangstDatum { get; set; }

    [Column("verzenddatum")]
    public DateOnly? VerzendDatum { get; set; }

    [Column("integriteit_algoritme")]
    public Algoritme Integriteit_Algoritme { get; set; }

    [MaxLength(50)]
    [Column("ondertekening_soort")]
    public Soort? Ondertekening_Soort { get; set; }

    [Column("ondertekening_datum")]
    public DateOnly? Ondertekening_Datum { get; set; }

    [MaxLength(128)]
    [Column("integriteit_waarde")]
    public string Integriteit_Waarde { get; set; }

    [Column("integriteit_datum")]
    public DateOnly? Integriteit_Datum { get; set; }

    [Column("verschijningsvorm")]
    public string Verschijningsvorm { get; set; }

    [Column("trefwoorden")]
    public List<string> Trefwoorden { get; set; } = [];

    [Column("inhoud_is_vervallen")]
    public bool InhoudIsVervallen { get; set; }

    [Column("enkelvoudiginformatieobject_lock_id")]
    public Guid EnkelvoudigInformatieObjectLockId { get; set; }

    // ZZZ
    //[Column("enkelvoudiginformatieobject_id ")]
    //public Guid EnkelvoudigInformatieObjectId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("informatieobjecttype")]
    public string InformatieObjectType { get; set; }

    [Column("indicatiegebruiksrecht")]
    public bool? IndicatieGebruiksrecht { get; set; }

    [Column("catalogus_id")]
    public Guid CatalogusId { get; set; }

    public EnkelvoudigInformatieObjectLock2 EnkelvoudigInformatieObjectLock { get; set; }

    //public EnkelvoudigInformatieObject InformatieObject { get; set; }

    //public EnkelvoudigInformatieObject LatestInformatieObject { get; set; }

    // ----

    public List<BestandsDeel> BestandsDelen { get; set; } = [];

    [MaxLength(250)]
    [Column("multipartdocument_id")]
    public string MultiPartDocumentId { get; set; }
}
