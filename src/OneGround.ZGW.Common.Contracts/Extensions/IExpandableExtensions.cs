using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Contracts.Extensions;

public static class IExpandableExtensions
{
    /// <summary>
    /// Reads a single expanded value out of <see cref="IExpandable.Expand"/> and converts it to <typeparamref name="TExpand"/>.
    /// Because the response is deserialized with Newtonsoft, a nested expand object ends up as a <see cref="JObject"/>
    /// (not a nested Dictionary), which this method converts for the caller.
    /// </summary>
    /// <param name="expandable">The (possibly expanded) response DTO.</param>
    /// <param name="path">The expand path, e.g. "catalogus".</param>
    /// <returns>The converted value, or <c>default</c> when the path wasn't requested/resolved.</returns>
    public static TExpand GetExpand<TExpand>(this IExpandable expandable, string path)
    {
        if (expandable?.Expand is null || !expandable.Expand.TryGetValue(path, out var value) || value is null)
        {
            return default;
        }

        return value is TExpand typed ? typed : ((JToken)value).ToObject<TExpand>();
    }
}
