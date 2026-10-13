namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Keys of the per-request caches that more than one resolver uses, built in one place so those resolvers really share their entries.
/// </summary>
internal static class ExpandCacheKeys
{
    /// <summary>
    /// The list of objectinformatieobjecten that DRC knows for a zaak (which documents the caller may see). Used by every resolver that
    /// checks the zaakinformatieobjecten of a zaak against DRC.
    /// </summary>
    public static string ObjectInformatieObjecten(string zaakUrl) => $"objectinformatieobjecten_{zaakUrl}";
}
