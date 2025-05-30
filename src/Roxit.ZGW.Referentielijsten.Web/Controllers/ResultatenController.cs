using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Referentielijsten.Contracts.v1.Responses;
using Roxit.ZGW.Referentielijsten.Web.Configuration;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;
using Roxit.ZGW.Referentielijsten.Web.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Referentielijsten.Web.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class ResultatenController : ControllerBase
{
    private readonly ILogger<ResultatenController> _logger;
    private readonly ReferentielijstenDataService _dataService;
    private readonly IMapper _mapper;
    private readonly IPaginationHelper _paginationHelper;
    private readonly IErrorResponseBuilder _errorResponseBuilder;
    private readonly IEntityUriService _uriService;
    private readonly ApplicationConfiguration _configuration;

    public ResultatenController(
        ILogger<ResultatenController> logger,
        ReferentielijstenDataService dataService,
        IMapper mapper,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        IEntityUriService uriService,
        IOptions<ApplicationConfiguration> options
    )
    {
        _logger = logger;
        _dataService = dataService;
        _mapper = mapper;
        _paginationHelper = paginationHelper;
        _errorResponseBuilder = errorResponseBuilder;
        _uriService = uriService;
        _configuration = options.Value;
    }

    /// <summary>
    /// Alle RESULTATEN opvragen.
    /// </summary>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Resultaten.GetAll, Name = Operations.Resultaten.List)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ResultaatResponseDto>))]
    public IActionResult GetAllAsync([FromQuery] GetAllResultatenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _configuration.ResultatenPageSize));

        var data = _dataService
            .Resultaten.Values.Where(r =>
                _uriService.GetId(r.ProcesType) == _uriService.GetId(queryParameters.ProcesType) || queryParameters.ProcesType == null
            )
            .ToList();

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, data.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var pagedResult = data.OrderBy(r => r.Url).Skip(_configuration.ResultatenPageSize * (page - 1)).Take(_configuration.ResultatenPageSize);

        var result = _mapper.Map<List<ResultaatResponseDto>>(pagedResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, result, data.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek RESULTAAT opvragen.
    /// </summary>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Resultaten.Get, Name = Operations.Resultaten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatResponseDto))]
    public IActionResult GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var exists = _dataService.Resultaten.TryGetValue(id, out var resultaat);

        if (!exists)
            return NotFound();

        return Ok(_mapper.Map<ResultaatResponseDto>(resultaat));
    }
}
