using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1._3;
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
public class RolTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<RolTypeResponseDto> _expandValidator;
    private readonly ExpandEngine<RolTypeResponseDto> _expandEngine;

    public RolTypeController(
        ILogger<RolTypeController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        ExpandValidator<RolTypeResponseDto> expandValidator,
        ExpandEngine<RolTypeResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Een specifieke ROLTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.RolTypen.Get, Name = Operations.RolTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(RolTypeResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetRolTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetRolTypeQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetRolTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<RolTypeResponseDto>(result.Result);

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
    /// De headers voor een specifiek(e) ROLTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.RolTypen.Get, Name = Operations.RolTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetRolTypeQueryParameters>))]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetRolTypeQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }

    /// <summary>
    /// Alle ROLTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.RolTypen.GetAll, Name = Operations.RolTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<RolTypeResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllRolTypenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllRolTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: "all", out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.RolTypenPageSize));
        var filter = _mapper.Map<GetAllRolTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllRolTypenQuery { GetAllRolTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var roltypenResponse = _mapper.Map<List<RolTypeResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(roltypenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, roltypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }
}
