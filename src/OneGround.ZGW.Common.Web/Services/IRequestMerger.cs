﻿using System;
using AutoMapper;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Services;

public interface IRequestMerger
{
    bool TryMergeValidity(IValidityEntity entity, object partialObjectRequest);
    TRequest MergePartialUpdateToObjectRequest<TRequest, TEntity>(
        TEntity existingObject,
        object partialObjectRequest,
        Action<IMappingOperationOptions<TEntity, TRequest>> opts = null
    )
        where TEntity : IBaseEntity;
}
