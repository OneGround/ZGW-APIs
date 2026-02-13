using System.Collections.Generic;
using AutoMapper;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Handlers;

public interface IEnkelvoudigInformatieObjectMerger
{
    EnkelvoudigInformatieObjectVersie TryMergeWithPartial(
        dynamic partialEnkelvoudigInformatieObject,
        EnkelvoudigInformatieObject enkelvoudigInformatieObject,
        List<ValidationError> errors
    );
}

public interface IEnkelvoudigInformatieObjectMergerFactory
{
    IEnkelvoudigInformatieObjectMerger Create<TRequestDto>()
        where TRequestDto : class;
}

public class EnkelvoudigInformatieObjectMergerFactory : IEnkelvoudigInformatieObjectMergerFactory
{
    private readonly IValidatorService _validatorService;
    private readonly IMapper _mapper;
    private readonly IRequestMerger _requestMerger;

    public EnkelvoudigInformatieObjectMergerFactory(IValidatorService validatorService, IMapper mapper, IRequestMerger requestMerger)
    {
        _validatorService = validatorService;
        _mapper = mapper;
        _requestMerger = requestMerger;
    }

    public IEnkelvoudigInformatieObjectMerger Create<TRequestDto>()
        where TRequestDto : class
    {
        return new EnkelvoudigInformatieObjectMerger<TRequestDto>(_validatorService, _mapper, _requestMerger);
    }
}

// Note: We could not use the GenericObjectMerger due to the fact that the return type of TryMergeWithPartial is different (EnkelvoudigInformatieObjectVersie while parameter passed is of type EnkelvoudigInformatieObject).
//  With this class we can merge all versions (1.0, 1.1, 1.5, etc.) of EnkelvoudigInformatieObjectRequest DTO's while the GenericObjectMerger can be used for other DTO's like VerzendingRequestDto and GebruiksRechtRequestDto
public class EnkelvoudigInformatieObjectMerger<TEnkelvoudigInformatieObjectRequestDto> : IEnkelvoudigInformatieObjectMerger
    where TEnkelvoudigInformatieObjectRequestDto : class
{
    private readonly IValidatorService _validatorService;
    private readonly IMapper _mapper;
    private readonly IRequestMerger _requestMerger;

    public EnkelvoudigInformatieObjectMerger(IValidatorService validatorService, IMapper mapper, IRequestMerger requestMerger)
    {
        _validatorService = validatorService;
        _mapper = mapper;
        _requestMerger = requestMerger;
    }

    public EnkelvoudigInformatieObjectVersie TryMergeWithPartial(
        dynamic partialEnkelvoudigInformatieObject,
        EnkelvoudigInformatieObject enkelvoudigInformatieObject,
        List<ValidationError> errors
    )
    {
        TEnkelvoudigInformatieObjectRequestDto mergedEnkelvoudigInformatieObjectDto = _requestMerger.MergePartialUpdateToObjectRequest<
            TEnkelvoudigInformatieObjectRequestDto,
            EnkelvoudigInformatieObject
        >(enkelvoudigInformatieObject, partialEnkelvoudigInformatieObject);

        if (!_validatorService.IsValid(mergedEnkelvoudigInformatieObjectDto, out var validationResult))
        {
            errors.AddRange(validationResult.ToValidationErrors());
            return null;
        }

        var enkelvoudigInformatieObjectVersieEntity = _mapper.Map<EnkelvoudigInformatieObjectVersie>(mergedEnkelvoudigInformatieObjectDto);

        return enkelvoudigInformatieObjectVersieEntity;
    }
}
