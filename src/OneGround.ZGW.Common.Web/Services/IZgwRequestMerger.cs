using System;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

/// <summary>
/// Applies a partial (PATCH) request over an existing entity: maps the entity to its request DTO, then
/// merges the caller's partial payload onto it.
/// </summary>
/// <remarks>
/// The pre-merge hook is a plain <c>Action&lt;TRequest&gt;</c> rather than any mapper's own options type,
/// so this contract stays independent of the mapper behind it.
/// </remarks>
public interface IZgwRequestMerger
{
    bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest);

    /// <param name="afterMap">
    /// Optional callback invoked on the mapped request after the entity-to-request map completes but
    /// before the partial-update merge runs. Some request DTOs conditionally serialize a property based
    /// on other property values (see <c>ShouldSerialize*</c> methods); if the merge base needs one of
    /// those conditional properties included, it must be set here, before the pre-merge serialization
    /// happens, not on the value this method returns.
    /// </param>
    TRequest MergePartialUpdateToObjectRequest<TRequest, TEntity>(
        TEntity existingObject,
        object partialObjectRequest,
        Action<TRequest> afterMap = null
    )
        where TEntity : IBaseEntity;
}
