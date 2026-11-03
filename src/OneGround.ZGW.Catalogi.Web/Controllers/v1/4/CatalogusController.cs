using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
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
using OneGround.ZGW.Catalogi.Web.Models.v1;
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
public class CatalogusController : ZGWControllerBase
{
    // The expand paths of this resource, see the resolvers registered in ExpandsServiceCollectionExtensions
    private const string AllowedExpand = "zaaktypen,besluittypen,informatieobjecttypen";

    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IPaginationHelper _paginationHelper;
    private readonly ExpandValidator<CatalogusResponseDto> _expandValidator;
    private readonly ExpandEngine<CatalogusResponseDto> _expandEngine;

    public CatalogusController(
        ILogger<CatalogusController> logger,
        IMediator mediator,
        IMapper mapper,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<CatalogusResponseDto> expandValidator,
        ExpandEngine<CatalogusResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _paginationHelper = paginationHelper;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
    }

    /// <summary>
    /// Alle CATALOGUSsen opvragen.
    /// </summary>
    /// <remarks>Deze lijst kan gefilterd wordt met query-string parameters.</remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Catalogussen.GetAll, Name = Operations.Catalogussen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<CatalogusResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllCatalogussenQueryParameters>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllCatalogussenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: AllowedExpand, out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.CatalogussenPageSize));
        var filter = _mapper.Map<GetAllCatalogussenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllCatalogussenQuery() { GetAllCatalogussenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var catalogussenResponse = _mapper.Map<List<CatalogusResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTOs. This is done after the mapping to the DTOs, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(catalogussenResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, catalogussenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke CATALOGUS opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Catalogussen.Get, Name = Operations.Catalogussen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(CatalogusResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetCatalogusQueryParameters>))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetCatalogusQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(_expandValidator, queryParameters.Expand, allowedExpand: AllowedExpand, out var expandPaths);
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new GetCatalogusQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var catalogusResponse = _mapper.Map<CatalogusResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(catalogusResponse, expandPaths);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        return Ok(catalogusResponse);
    }

    /// <summary>
    /// De headers voor een specifiek(e) CATALOGUS opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.Catalogussen.Get, Name = Operations.Catalogussen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetCatalogusQueryParameters>))]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetCatalogusQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }
}
