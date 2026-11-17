using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1._4;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Catalogi.Web.Controllers.v1._4;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_4)]
public class ZaakTypeInformatieObjectTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IRequestMerger _requestMerger;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ZaakTypeInformatieObjectTypeResponseDto> _expandValidator;
    private readonly ExpandEngine<ZaakTypeInformatieObjectTypeResponseDto> _expandEngine;

    public ZaakTypeInformatieObjectTypeController(
        ILogger<ZaakTypeInformatieObjectTypeController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        ExpandValidator<ZaakTypeInformatieObjectTypeResponseDto> expandValidator,
        ExpandEngine<ZaakTypeInformatieObjectTypeResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _requestMerger = requestMerger;
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Een specifieke ZAAKTYPE-INFORMATIEOBJECTTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypeInformatieObjectTypen.Get, Name = Operations.ZaakTypeInformatieObjectTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakTypeInformatieObjectTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetZaakTypeInformatieObjectTypeQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetZaakTypeInformatieObjectTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(response, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        return Ok(response);
    }

    /// <summary>
    /// Alle ZAAKTYPE-INFORMATIEOBJECTTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypeInformatieObjectTypen.GetAll, Name = Operations.ZaakTypeInformatieObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakTypeInformatieObjectTypenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakTypeInformatieObjectTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakTypeInformatieObjectTypenPageSize));
        var filter = _mapper.Map<GetAllZaakTypeInformatieObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakTypeInformatieObjectTypenQuery { GetAllZaakTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakTypeInformatieObjectTypenResponse = _mapper.Map<List<ZaakTypeInformatieObjectTypeResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(zaakTypeInformatieObjectTypenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            zaakTypeInformatieObjectTypenResponse,
            result.Result.Count
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Create ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <param name="zaakTypeInformatieObjectTypeRequest"></param>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakTypeInformatieObjectTypen.Create, Name = Operations.ZaakTypeInformatieObjectTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakTypeInformatieObjectTypeRequestDto zaakTypeInformatieObjectTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakTypeInformatieObjectTypeRequest);

        var zaakTypeInformatieObjectType = _mapper.Map<ZaakTypeInformatieObjectType>(zaakTypeInformatieObjectTypeRequest);

        var result = await _mediator.Send(
            new CreateZaakTypeInformatieObjectTypenCommand
            {
                ZaakTypeInformatieObjectType = zaakTypeInformatieObjectType,
                ZaakType = zaakTypeInformatieObjectTypeRequest.ZaakType,
                StatusType = zaakTypeInformatieObjectTypeRequest.StatusType,
            }
        );

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = await GetResponseAsync(result.Result.Id);

        if (response == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Created(response.Url, response);
    }

    /// <summary>
    /// Update ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakTypeInformatieObjectTypen.Update, Name = Operations.ZaakTypeInformatieObjectTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakTypeInformatieObjectTypeRequestDto request, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), request, id);

        var zaakTypeInformatieObjectType = _mapper.Map<ZaakTypeInformatieObjectType>(request);

        var result = await _mediator.Send(
            new UpdateZaakTypeInformatieObjectTypeCommand()
            {
                ZaakTypeInformatieObjectType = zaakTypeInformatieObjectType,
                ZaakType = request.ZaakType,
                InformatieObjectType = request.InformatieObjectType,
                StatusType = request.StatusType,
                Id = id,
                IsPartialUpdate = false,
            }
        );

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var statusTypeResponse = await GetResponseAsync(result.Result.Id);

        if (statusTypeResponse == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(statusTypeResponse);
    }

    /// <summary>
    /// Partially update ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakTypeInformatieObjectTypen.Update, Name = Operations.ZaakTypeInformatieObjectTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakTypeInformatieObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakTypeInformatieObjectTypeQuery() { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ZaakTypeInformatieObjectTypeRequestDto mergedZaakTypeInformatieObjectTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            ZaakTypeInformatieObjectTypeRequestDto,
            ZaakTypeInformatieObjectType
        >(resultGet.Result, partialZaakTypeInformatieObjectTypeRequest);

        if (!_validatorService.IsValid(mergedZaakTypeInformatieObjectTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakTypeInformatieObjectType mergedZaakTypeInformatieObjectType = _mapper.Map<ZaakTypeInformatieObjectType>(
            mergedZaakTypeInformatieObjectTypeRequest
        );

        var resultUpd = await _mediator.Send(
            new UpdateZaakTypeInformatieObjectTypeCommand()
            {
                Id = id,
                ZaakTypeInformatieObjectType = mergedZaakTypeInformatieObjectType,
                StatusType = mergedZaakTypeInformatieObjectTypeRequest.StatusType,
                ZaakType = mergedZaakTypeInformatieObjectTypeRequest.ZaakType,
                InformatieObjectType = mergedZaakTypeInformatieObjectTypeRequest.InformatieObjectType,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound(resultUpd.Errors);
        }

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var zaakTypeInformatieObjectTypeResponseDto = await GetResponseAsync(resultUpd.Result.Id);

        if (zaakTypeInformatieObjectTypeResponseDto == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(zaakTypeInformatieObjectTypeResponseDto);
    }

    private async Task<ZaakTypeInformatieObjectTypeResponseDto> GetResponseAsync(Guid id)
    {
        var result = await _mediator.Send(new GetZaakTypeInformatieObjectTypeQuery { Id = id });

        // Note: Read it back through the read query, so the response is the same as of the read operations (including the lists that are new in 1.4)
        return result.Status == QueryStatus.NotFound ? null : _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(result.Result);
    }
}
