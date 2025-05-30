using System;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Common.Web.Services;

public class RequestMerger : IRequestMerger
{
    private readonly JsonSerializer _jsonSerializer = new ZGWJsonSerializer();
    private readonly IMapper _mapper;

    public RequestMerger(IMapper mapper)
    {
        _mapper = mapper;
    }

    public bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest)
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

    public TRequest MergePartialUpdateToObjectRequest<TRequest, TEntity>(
        TEntity existingObject,
        object partialObjectRequest,
        Action<IMappingOperationOptions<TEntity, TRequest>> opts = null
    )
        where TEntity : IBaseEntity
    {
        if (partialObjectRequest is not JObject objectRequest)
        {
            throw new InvalidOperationException($"{partialObjectRequest} is not JObject");
        }

        TRequest existingObjectRequest;
        if (opts == null)
            existingObjectRequest = _mapper.Map<TRequest>(existingObject);
        else
            existingObjectRequest = _mapper.Map<TEntity, TRequest>(existingObject, opts);

        // Apply JSON merge
        var joExistingObjectRequest = JObject.FromObject(existingObjectRequest, _jsonSerializer);
        joExistingObjectRequest.Merge(
            objectRequest,
            new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
                MergeNullValueHandling = MergeNullValueHandling.Merge,
            }
        );

        TRequest mergedObjectRequest = joExistingObjectRequest.ToObject<TRequest>(_jsonSerializer);

        return mergedObjectRequest;
    }
}
