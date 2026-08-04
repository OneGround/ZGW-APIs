using System;
using AutoMapper;
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
        Action<IMappingOperationOptions<TEntity, TRequest>> opts = null
    )
        where TEntity : IBaseEntity
    {
        var objectRequest = PartialUpdateMerger.AsJObject(partialObjectRequest);

        TRequest existingObjectRequest;
        if (opts == null)
            existingObjectRequest = _mapper.Map<TRequest>(existingObject);
        else
            existingObjectRequest = _mapper.Map<TEntity, TRequest>(existingObject, opts);

        return _merger.Merge(existingObjectRequest, objectRequest);
    }
}
