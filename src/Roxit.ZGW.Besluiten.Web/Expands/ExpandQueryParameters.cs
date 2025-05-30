namespace Roxit.ZGW.Besluiten.Web.Expands;

public static class ExpanderNames
{
    public const string BesluitExpander = "besluit";
}

public static class ExpandKeys
{
    //besluittype
    public const string BesluitType = "besluittype";
    public const string Catalogus = "catalogus";

    //besluitinformatieobjecten
    public const string BesluitInformatieObjecten = "besluitinformatieobjecten";
    public const string InformatieObject = "informatieobject";
    public const string InformatieObjectType = "informatieobjecttype";
}

public static class ExpandQueries
{
    public static readonly string BesluitType_Catalogus = $"{ExpandKeys.BesluitType}.{ExpandKeys.Catalogus}";
    public static readonly string BesluitInformatieObjecten_InformatieObject =
        $"{ExpandKeys.BesluitInformatieObjecten}.{ExpandKeys.InformatieObject}";
    public static readonly string BesluitInformatieObjecten_InformatieObject_InformatieObjectType =
        $"{BesluitInformatieObjecten_InformatieObject}.{ExpandKeys.InformatieObjectType}";
}
