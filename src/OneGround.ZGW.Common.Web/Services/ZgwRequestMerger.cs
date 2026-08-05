using MapsterMapper;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

public class ZgwRequestMerger : IZgwRequestMerger
{
    private readonly PartialUpdateMerger _merger = new PartialUpdateMerger();
    private readonly IMapper _mapper;

    public ZgwRequestMerger(IMapper mapper)
    {
        _mapper = mapper;
    }

    public bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest) =>
        PartialUpdateMerger.TryMergeValidity(entity, partialObjectRequest);

    public TRequest MergePartialUpdateToObjectRequest<TRequest, TEntity>(TEntity existingObject, object partialObjectRequest)
        where TEntity : IBaseEntity
    {
        var objectRequest = PartialUpdateMerger.AsJObject(partialObjectRequest);

        var existingObjectRequest = _mapper.Map<TRequest>(existingObject);

        return _merger.Merge(existingObjectRequest, objectRequest);
    }
}
