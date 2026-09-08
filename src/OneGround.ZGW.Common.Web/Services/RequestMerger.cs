using System;
using MapsterMapper;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

public class RequestMerger : IRequestMerger
{
    private readonly PartialUpdateMerger _merger = new PartialUpdateMerger();
    private readonly IMapper _mapper;

    public RequestMerger(IMapper mapper)
    {
        _mapper = mapper;
    }

    public bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest) =>
        PartialUpdateMerger.TryMergeValidity(entity, partialObjectRequest);

    public TRequest MergePartialUpdateToObjectRequest<TRequest, TEntity>(
        TEntity existingObject,
        object partialObjectRequest,
        Action<TRequest> afterMap = null
    )
        where TEntity : IBaseEntity
    {
        var objectRequest = PartialUpdateMerger.AsJObject(partialObjectRequest);

        var existingObjectRequest = _mapper.Map<TRequest>(existingObject);

        // Must run before Merge(): Merge() serializes existingObjectRequest to build the merge base, and
        // some request DTOs conditionally serialize a property based on other property values on the same
        // object. Applying afterMap here, before that serialization, lets a caller opt such a property into
        // the merge base; applying it to the value this method returns would be too late.
        afterMap?.Invoke(existingObjectRequest);

        return _merger.Merge(existingObjectRequest, objectRequest);
    }
}
