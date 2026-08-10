using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

/// <summary>
/// Mapster-side counterpart to <see cref="IRequestMerger"/>, for services that have adopted Mapster.
/// </summary>
/// <remarks>
/// This exists as a separate contract rather than a change to <see cref="IRequestMerger"/> because that
/// interface is consumed outside this repository and its signature exposes AutoMapper's
/// <c>IMappingOperationOptions</c>, which no mapper-agnostic abstraction can honour. The options
/// parameter is deliberately absent here: only one caller in this repository ever used it.
/// <c>TryMergeValidity</c> is duplicated onto this contract (it needs no mapper) so a migrated service
/// never has to inject both mergers.
/// </remarks>
public interface IZgwRequestMerger
{
    bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest);

    TRequest MergePartialUpdateToObjectRequest<TRequest, TEntity>(TEntity existingObject, object partialObjectRequest)
        where TEntity : IBaseEntity;
}
