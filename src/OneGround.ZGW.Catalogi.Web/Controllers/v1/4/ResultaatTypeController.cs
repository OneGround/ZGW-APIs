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
public class ResultaatTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IRequestMerger _requestMerger;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ResultaatTypeResponseDto> _expandValidator;
    private readonly ExpandEngine<ResultaatTypeResponseDto> _expandEngine;

    public ResultaatTypeController(
        ILogger<ResultaatTypeController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        ExpandValidator<ResultaatTypeResponseDto> expandValidator,
        ExpandEngine<ResultaatTypeResponseDto> expandEngine
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
    /// Een specifieke RESULTAATTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ResultaatTypen.Get, Name = Operations.ResultaatTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetResultaatTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetResultaatTypeQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetResultaatTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ResultaatTypeResponseDto>(result.Result);

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
    /// De headers voor een specifiek(e) RESULTAATTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ResultaatTypen.Get, Name = Operations.ResultaatTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetResultaatTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetResultaatTypeQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }

    /// <summary>
    /// Alle RESULTAATTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ResultaatTypen.GetAll, Name = Operations.ResultaatTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ResultaatTypeResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllResultaatTypenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllResultaatTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ResultaatTypenPageSize));
        var filter = _mapper.Map<GetAllResultaatTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllResultaatTypenQuery { GetAllResultaatTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var resultaattypenResponse = _mapper.Map<List<ResultaatTypeResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(resultaattypenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, resultaattypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Maak een RESULTAATTYPE aan.
    /// </summary>
    /// <remarks>
    /// Maak een RESULTAATTYPE aan. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ResultaatTypen.Create, Name = Operations.ResultaatTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ResultaatTypeRequestDto resultTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), resultTypeRequest);

        ResultaatType resultType = _mapper.Map<ResultaatType>(resultTypeRequest);

        var result = await _mediator.Send(
            new CreateResultaatTypeCommand
            {
                ResultaatType = resultType,
                ZaakType = resultTypeRequest.ZaakType,
                BesluitTypen = resultTypeRequest.BesluitTypen,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var resultTypeResponse = await GetResponseAsync(result.Result.Id);

        if (resultTypeResponse == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Created(resultTypeResponse.Url, resultTypeResponse);
    }

    /// <summary>
    /// Werk een RESULTAATTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een RESULTAATTYPE in zijn geheel bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ResultaatTypen.Update, Name = Operations.ResultaatTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ResultaatTypeRequestDto resultTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), resultTypeRequest, id);

        ResultaatType resultType = _mapper.Map<ResultaatType>(resultTypeRequest);

        var result = await _mediator.Send(
            new UpdateResultaatTypeCommand
            {
                ResultaatType = resultType,
                Id = id,
                ZaakType = resultTypeRequest.ZaakType,
                BesluitTypen = resultTypeRequest.BesluitTypen,
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

        var resultTypeResponse = await GetResponseAsync(result.Result.Id);

        if (resultTypeResponse == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(resultTypeResponse);
    }

    /// <summary>
    /// Werk een RESULTAATTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een RESULTAATTYPE deels bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ResultaatTypen.Update, Name = Operations.ResultaatTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialResultTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetResultaatTypeQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ResultaatTypeRequestDto mergedResultTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<ResultaatTypeRequestDto, ResultaatType>(
            resultGet.Result,
            partialResultTypeRequest
        );

        if (!_validatorService.IsValid(mergedResultTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ResultaatType mergedResultType = _mapper.Map<ResultaatType>(mergedResultTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateResultaatTypeCommand
            {
                ResultaatType = mergedResultType,
                Id = id,
                ZaakType = mergedResultTypeRequest.ZaakType,
                BesluitTypen = mergedResultTypeRequest.BesluitTypen,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var response = await GetResponseAsync(resultUpd.Result.Id);

        if (response == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(response);
    }

    private async Task<ResultaatTypeResponseDto> GetResponseAsync(Guid id)
    {
        var result = await _mediator.Send(new GetResultaatTypeQuery { Id = id });

        // Note: Read it back through the read query, so the response is the same as of the read operations (including the lists that are new in 1.4)
        return result.Status == QueryStatus.NotFound ? null : _mapper.Map<ResultaatTypeResponseDto>(result.Result);
    }
}
