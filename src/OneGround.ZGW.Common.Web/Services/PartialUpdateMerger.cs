using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

/// <summary>
/// The mapper-agnostic half of a partial (PATCH) update. Shared by RequestMerger (AutoMapper) and
/// ZgwRequestMerger (Mapster) so the two cannot drift in how they merge — the only difference between
/// them is which mapper produces the request DTO from the existing entity.
/// </summary>
internal sealed class PartialUpdateMerger
{
    private readonly JsonSerializer _jsonSerializer = new ZGWJsonSerializer();

    /// <summary>
    /// Validates the caller's partial request. Kept separate from <see cref="Merge"/> so both mergers can
    /// run it BEFORE mapping, preserving the original ordering in which an invalid payload throws without
    /// a map having been attempted.
    /// </summary>
    public static JObject AsJObject(object partialObjectRequest)
    {
        if (partialObjectRequest is not JObject objectRequest)
        {
            throw new InvalidOperationException($"{partialObjectRequest} is not JObject");
        }

        return objectRequest;
    }

    // A PATCH containing only eindeGeldigheid is common enough (closing off validity without touching
    // anything else) to warrant its own fast path: it updates that one field directly on the entity and
    // reports handled, letting the caller skip mapping/merging the whole request DTO for a single field.
    public static bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest)
    {
        var objectRequest = partialObjectRequest as JObject;
        if (objectRequest?.Count == 1)
        {
            var token = objectRequest.SelectToken("eindeGeldigheid");
            if (token != null)
            {
                var date = token.ToObject<DateTime?>(); //Newtonsoft fails to cast to DateOnly
                entity.EindeGeldigheid = date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
                return true;
            }
        }

        return false;
    }

    // MergeArrayHandling.Replace: a PATCHed array is the caller's full intended array, not entries to
    // append. PropertyNameComparison.OrdinalIgnoreCase: the request DTO's PascalCase properties must
    // still match the incoming payload's camelCase JSON keys. MergeNullValueHandling.Merge: a JSON
    // `null` in the patch is an explicit instruction to clear that field, so it must overwrite the
    // existing value rather than be skipped as "no value provided".
    public TRequest Merge<TRequest>(TRequest existingObjectRequest, JObject partialObjectRequest)
    {
        var joExistingObjectRequest = JObject.FromObject(existingObjectRequest, _jsonSerializer);
        joExistingObjectRequest.Merge(
            partialObjectRequest,
            new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
                MergeNullValueHandling = MergeNullValueHandling.Merge,
            }
        );

        return joExistingObjectRequest.ToObject<TRequest>(_jsonSerializer);
    }
}
