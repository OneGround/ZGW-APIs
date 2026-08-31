using System;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

/// <summary>
/// Mapster-side counterpart to <see cref="IRequestMerger"/>, for services that have adopted Mapster.
/// </summary>
/// <remarks>
/// This exists as a separate contract rather than a change to <see cref="IRequestMerger"/> because that
/// interface is consumed outside this repository and its signature exposes AutoMapper's
/// <c>IMappingOperationOptions</c>, which no mapper-agnostic abstraction can honour. In place of that,
/// this contract takes a plain, mapper-agnostic <c>Action&lt;TRequest&gt;</c> for the one caller in this
/// repository that needs to touch the mapped request before it is merged.
/// <c>TryMergeValidity</c> is duplicated onto this contract (it needs no mapper) so a migrated service
/// never has to inject both mergers.
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
