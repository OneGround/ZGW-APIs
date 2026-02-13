using System.Collections.Generic;
using AutoMapper;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.Web.Handlers;

public interface IGenericObjectMerger
{
    TEntity TryMergeWithPartial<TEntity>(
        dynamic partialEnkelvoudigInformatieObjectRequest,
        TEntity enkelvoudigInformatieObject,
        List<ValidationError> errors
    )
        where TEntity : IBaseEntity;
}

public interface IGenericObjectMergerFactory
{
    IGenericObjectMerger Create<TRequestDto>()
        where TRequestDto : class;
}

public class GenericObjectMergerFactory : IGenericObjectMergerFactory
{
    private readonly IValidatorService _validatorService;
    private readonly IMapper _mapper;
    private readonly IRequestMerger _requestMerger;

    public GenericObjectMergerFactory(IValidatorService validatorService, IMapper mapper, IRequestMerger requestMerger)
    {
        _validatorService = validatorService;
        _mapper = mapper;
        _requestMerger = requestMerger;
    }

    public IGenericObjectMerger Create<TRequestDto>()
        where TRequestDto : class
    {
        return new GenericObjectMerger<TRequestDto>(_validatorService, _mapper, _requestMerger);
    }
}

public class GenericObjectMerger<TRequestDto> : IGenericObjectMerger
    where TRequestDto : class
{
    private readonly IValidatorService _validatorService;
    private readonly IMapper _mapper;
    private readonly IRequestMerger _requestMerger;

    public GenericObjectMerger(IValidatorService validatorService, IMapper mapper, IRequestMerger requestMerger)
    {
        _validatorService = validatorService;
        _mapper = mapper;
        _requestMerger = requestMerger;
    }

    public TEntity TryMergeWithPartial<TEntity>(dynamic partialDtoObject, TEntity entityObject, List<ValidationError> errors)
        where TEntity : IBaseEntity
    {
        TRequestDto mergedDtoObject = _requestMerger.MergePartialUpdateToObjectRequest<TRequestDto, TEntity>(entityObject, partialDtoObject);

        if (!_validatorService.IsValid(mergedDtoObject, out var validationResult))
        {
            errors.AddRange(validationResult.ToValidationErrors());
            return default;
        }

        var mergedEntityObject = _mapper.Map<TEntity>(mergedDtoObject);

        return mergedEntityObject;
    }
}
