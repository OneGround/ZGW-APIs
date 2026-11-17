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
public class ZaakObjectTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IRequestMerger _requestMerger;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ZaakObjectTypeResponseDto> _expandValidator;
    private readonly ExpandEngine<ZaakObjectTypeResponseDto> _expandEngine;

    public ZaakObjectTypeController(
        ILogger<ZaakObjectTypeController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        ExpandValidator<ZaakObjectTypeResponseDto> expandValidator,
        ExpandEngine<ZaakObjectTypeResponseDto> expandEngine
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
    /// Een specifieke ZAAKOBJECTTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjectTypen.Get, Name = Operations.ZaakObjectTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectTypeResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakObjectTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetZaakObjectTypeQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetZaakObjectTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ZaakObjectTypeResponseDto>(result.Result);

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
    /// De headers voor een specifiek(e) ZAAKOBJECTTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakObjectTypen.Get, Name = Operations.ZaakObjectTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakObjectTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetZaakObjectTypeQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }

    /// <summary>
    /// Alle ZAAKOBJECTTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjectTypen.GetAll, Name = Operations.ZaakObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakObjectTypeResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakObjectTypenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakObjectTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakObjectTypenPageSize));
        var filter = _mapper.Map<GetAllZaakObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakObjectTypenQuery { GetAllZaakObjectTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakobjecttypenResponse = _mapper.Map<List<ZaakObjectTypeResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(zaakobjecttypenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaakobjecttypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Maak een ZAAKOBJECTTYPE aan.
    /// </summary>
    /// <remarks>
    /// Maak een ZAAKOBJECTTYPE aan. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakObjectTypen.Create, Name = Operations.ZaakObjectTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakObjectTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakObjectTypeRequestDto zaakObjectTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakObjectTypeRequest);

        ZaakObjectType zaakObjectType = _mapper.Map<ZaakObjectType>(zaakObjectTypeRequest);

        var result = await _mediator.Send(
            new CreateZaakObjectTypeCommand { ZaakObjectType = zaakObjectType, ZaakType = zaakObjectTypeRequest.ZaakType }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var zaakObjectTypeResponse = await GetResponseAsync(result.Result.Id);

        if (zaakObjectTypeResponse == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Created(zaakObjectTypeResponse.Url, zaakObjectTypeResponse);
    }

    /// <summary>
    /// Werk een ZAAKOBJECTTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een ZAAKOBJECTTYPE in zijn geheel bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakObjectTypen.Update, Name = Operations.ZaakObjectTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakObjectTypeRequestDto zaakObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakObjectTypeRequest, id);

        ZaakObjectType zaakObjectType = _mapper.Map<ZaakObjectType>(zaakObjectTypeRequest);

        var result = await _mediator.Send(
            new UpdateZaakObjectTypeCommand
            {
                ZaakObjectType = zaakObjectType,
                Id = id,
                ZaakType = zaakObjectTypeRequest.ZaakType,
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

        var zaakObjectTypeResponse = await GetResponseAsync(result.Result.Id);

        if (zaakObjectTypeResponse == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(zaakObjectTypeResponse);
    }

    /// <summary>
    /// Werk een ZAAKOBJECTTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een ZAAKOBJECTTYPE deels bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakObjectTypen.Update, Name = Operations.ZaakObjectTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakObjectTypeQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ZaakObjectTypeRequestDto mergedZaakObjectTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            ZaakObjectTypeRequestDto,
            ZaakObjectType
        >(resultGet.Result, partialZaakObjectTypeRequest);

        if (!_validatorService.IsValid(mergedZaakObjectTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakObjectType mergedZaakObjectType = _mapper.Map<ZaakObjectType>(mergedZaakObjectTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakObjectTypeCommand
            {
                ZaakObjectType = mergedZaakObjectType,
                Id = id,
                ZaakType = mergedZaakObjectTypeRequest.ZaakType,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var zaakObjectTypeResponse = await GetResponseAsync(resultUpd.Result.Id);

        if (zaakObjectTypeResponse == null)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(zaakObjectTypeResponse);
    }

    private async Task<ZaakObjectTypeResponseDto> GetResponseAsync(Guid id)
    {
        var result = await _mediator.Send(new GetZaakObjectTypeQuery { Id = id });

        // Note: Read it back through the read query, so the response is the same as of the read operations (including the lists that are new in 1.4)
        return result.Status == QueryStatus.NotFound ? null : _mapper.Map<ZaakObjectTypeResponseDto>(result.Result);
    }
}
