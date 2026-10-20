using System;

namespace OneGround.ZGW.Zaken.DataModel;

public enum InnRechtsvorm
{
    besloten_vennootschap,
    cooperatie_europees_economische_samenwerking,

    [Obsolete("Typefout in enum innRechtsvorm #2581. Use enum 'europese_cooperatieve_vennootschap' instead")]
    europese_cooperatieve_venootschap,

    // Note: stored as smallint. The explicit value anchors this member and the ones after it, so removing the deprecated member above later
    // does not shift their stored values.
    europese_naamloze_vennootschap = 3,
    kerkelijke_organisatie,
    naamloze_vennootschap,
    onderlinge_waarborg_maatschappij,
    overig_privaatrechtelijke_rechtspersoon,
    stichting,
    vereniging,
    vereniging_van_eigenaars,
    publiekrechtelijke_rechtspersoon,
    vennootschap_onder_firma,
    maatschap,
    rederij,
    commanditaire_vennootschap,
    kapitaalvennootschap_binnen_eer,
    overige_buitenlandse_rechtspersoon_vennootschap,
    kapitaalvennootschap_buiten_eer,

    // Fix Typefout in enum innRechtsvorm #2581
    europese_cooperatieve_vennootschap,
}
