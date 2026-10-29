using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel.ZaakObject;

[Table("zaakobjecten_terreingebouwdobjectzaakobjecten")]
public class TerreinGebouwdObjectZaakObject : OwnedEntity, IBaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("identificatie")]
    public string Identificatie { get; set; }

    [MaxLength(100)]
    [Column("adresaanduidinggrp_numidentificatie")]
    public string AdresAanduidingGrp_NumIdentificatie { get; set; }

    [MaxLength(100)]
    [Column("adresaanduidinggrp_oaoidentificatie")]
    public string AdresAanduidingGrp_OaoIdentificatie { get; set; }

    // Note: what the client sent under the deprecated (oao) and the correct (aoa) name of the identificatie of the adresAanduidingGrp (#2576). Not
    // stored: only the update uses them, to tell which of two differing names the client actually changed (see AdresAanduidingGrpIdentificatie).
    [NotMapped]
    public string AdresAanduidingGrp_OaoIdentificatieAsSent { get; set; }

    [NotMapped]
    public string AdresAanduidingGrp_AoaIdentificatieAsSent { get; set; }

    [MaxLength(80)]
    [Column("adresaanduidinggrp_wplwoonplaatsnaam")]
    public string AdresAanduidingGrp_WplWoonplaatsNaam { get; set; }

    [MaxLength(80)]
    [Column("adresaanduidinggrp_goropenbareruimtenaam")]
    public string AdresAanduidingGrp_GorOpenbareRuimteNaam { get; set; }

    [MaxLength(7)]
    [Column("adresaanduidinggrp_aoapostcode")]
    public string AdresAanduidingGrp_AoaPostcode { get; set; }

    [Column("adresaanduidinggrp_aoahuisnummer")]
    public int AdresAanduidingGrp_AoaHuisnummer { get; set; }

    [MaxLength(1)]
    [Column("adresaanduidinggrp_aoahuisletter")]
    public string AdresAanduidingGrp_AoaHuisletter { get; set; }

    [MaxLength(4)]
    [Column("adresaanduidinggrp_aoahuisnummertoevoeging")]
    public string AdresAanduidingGrp_AoaHuisnummertoevoeging { get; set; }

    [MaxLength(100)]
    [Column("adresaanduidinggrp_ogolocatieaanduiding")]
    public string AdresAanduidingGrp_OgoLocatieAanduiding { get; set; }

    [Column("zaakobject_id")]
    public Guid ZaakObjectId { get; set; }

    [ForeignKey("ZaakObjectId")]
    public ZaakObject ZaakObject { get; set; }

    [NotMapped]
    public bool IsAdresAanduidingGrp =>
        !(
            AdresAanduidingGrp_NumIdentificatie == null
            && AdresAanduidingGrp_OaoIdentificatie == null
            && AdresAanduidingGrp_WplWoonplaatsNaam == null
            && AdresAanduidingGrp_GorOpenbareRuimteNaam == null
            && AdresAanduidingGrp_AoaPostcode == null
            && AdresAanduidingGrp_AoaHuisnummer == 0
            && AdresAanduidingGrp_AoaHuisletter == null
            && AdresAanduidingGrp_AoaHuisnummertoevoeging == null
            && AdresAanduidingGrp_OgoLocatieAanduiding == null
        );
}
