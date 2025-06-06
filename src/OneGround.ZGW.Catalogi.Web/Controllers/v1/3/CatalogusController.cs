using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Catalogi.Web.Controllers.v1._3;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_3)]
public class CatalogusController : ZGWControllerBase
{
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IPaginationHelper _paginationHelper;

    public CatalogusController(
        ILogger<CatalogusController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _paginationHelper = paginationHelper;
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
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Catalogi.Contracts.v1.Queries.GetAllCatalogussenQueryParameters queryParameters,
        int page = 1
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.CatalogussenPageSize));
        var filter = _mapper.Map<Models.v1.GetAllCatalogussenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllCatalogussenQuery() { GetAllCatalogussenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var statustypenResponse = _mapper.Map<List<CatalogusResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, statustypenResponse, result.Result.Count);

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
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetCatalogusQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var catalogusResponse = _mapper.Map<CatalogusResponseDto>(result.Result);

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
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
    }

    /// <summary>
    /// Maak een CATALOGUS aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Catalogussen.Create, Name = Operations.Catalogussen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(CatalogusResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] CatalogusRequestDto catalogusRequestDto)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Rsin}", nameof(AddAsync), catalogusRequestDto, catalogusRequestDto.Rsin);

        Catalogus catalogus = _mapper.Map<Catalogus>(catalogusRequestDto);

        var result = await _mediator.Send(new CreateCatalogusCommand { Catalogus = catalogus });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<CatalogusResponseDto>(result.Result);

        return Created(response.Url, response);
    }
}
