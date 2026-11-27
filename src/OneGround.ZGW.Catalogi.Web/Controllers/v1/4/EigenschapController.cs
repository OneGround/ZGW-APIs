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
public class EigenschapController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IRequestMerger _requestMerger;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<EigenschapResponseDto> _expandValidator;
    private readonly ExpandEngine<EigenschapResponseDto> _expandEngine;

    public EigenschapController(
        ILogger<EigenschapController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        ExpandValidator<EigenschapResponseDto> expandValidator,
        ExpandEngine<EigenschapResponseDto> expandEngine
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
    /// Een specifieke EIGENSCHAP opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Eigenschappen.Get, Name = Operations.Eigenschappen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EigenschapResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetEigenschapQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetEigenschapQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetEigenschapQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<EigenschapResponseDto>(result.Result);

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
    /// De headers voor een specifiek(e) EIGENSCHAP opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.Eigenschappen.Get, Name = Operations.Eigenschappen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetEigenschapQueryParameters>))]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetEigenschapQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }

    /// <summary>
    /// Alle EIGENSCHAPpen opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Eigenschappen.GetAll, Name = Operations.Eigenschappen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<EigenschapResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllEigenschappenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllEigenschappenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.EigenschappenPageSize));
        var filter = _mapper.Map<GetAllEigenschappenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllEigenschappenQuery { GetAllEigenschappenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var eigenschappenResponse = _mapper.Map<List<EigenschapResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(eigenschappenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, eigenschappenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Maak een EIGENSCHAP aan.
    /// </summary>
    /// <remarks>
    /// Maak een EIGENSCHAP aan. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Eigenschappen.Create, Name = Operations.Eigenschappen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(EigenschapResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] EigenschapRequestDto eigenschapRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), eigenschapRequest);

        Eigenschap eigenschap = _mapper.Map<Eigenschap>(eigenschapRequest);

        var result = await _mediator.Send(
            new CreateEigenschapCommand
            {
                Eigenschap = eigenschap,
                ZaakType = eigenschapRequest.ZaakType,
                StatusType = eigenschapRequest.StatusType,
            }
        );

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
    /// Werk een EIGENSCHAP in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een EIGENSCHAP in zijn geheel bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.Eigenschappen.Update, Name = Operations.Eigenschappen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EigenschapResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] EigenschapRequestDto eigenschapRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), eigenschapRequest, id);

        Eigenschap eigenschap = _mapper.Map<Eigenschap>(eigenschapRequest);

        var result = await _mediator.Send(
            new UpdateEigenschapCommand
            {
                Eigenschap = eigenschap,
                Id = id,
                ZaakType = eigenschapRequest.ZaakType,
                StatusType = eigenschapRequest.StatusType,
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

        var response = await GetResponseAsync(result.Result.Id);

        if (response == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(response);
    }

    /// <summary>
    /// Werk een EIGENSCHAP deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een EIGENSCHAP deels bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.Eigenschappen.Update, Name = Operations.Eigenschappen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EigenschapResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialEigenschapRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetEigenschapQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        EigenschapRequestDto mergedEigenschapRequest = _requestMerger.MergePartialUpdateToObjectRequest<EigenschapRequestDto, Eigenschap>(
            resultGet.Result,
            partialEigenschapRequest
        );

        if (!_validatorService.IsValid(mergedEigenschapRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        Eigenschap mergedEigenschap = _mapper.Map<Eigenschap>(mergedEigenschapRequest);

        var resultUpd = await _mediator.Send(
            new UpdateEigenschapCommand
            {
                Eigenschap = mergedEigenschap,
                Id = id,
                ZaakType = mergedEigenschapRequest.ZaakType,
                StatusType = mergedEigenschapRequest.StatusType,
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

    private async Task<EigenschapResponseDto> GetResponseAsync(Guid id)
    {
        var result = await _mediator.Send(new GetEigenschapQuery { Id = id });

        // Note: Read it back through the read query, so the response is the same as of the read operations (including the lists that are new in 1.4)
        return result.Status == QueryStatus.NotFound ? null : _mapper.Map<EigenschapResponseDto>(result.Result);
    }
}
