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
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Referentielijsten.Contracts.v1.Responses;
using Roxit.ZGW.Referentielijsten.Web.Configuration;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1;
using Roxit.ZGW.Referentielijsten.Web.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Referentielijsten.Web.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class CommunicatieKanalenController : ControllerBase
{
    private readonly ReferentielijstenDataService _dataService;
    private readonly ILogger<CommunicatieKanalenController> _logger;
    private readonly ApplicationConfiguration _configuration;
    private readonly IPaginationHelper _paginationHelper;
    private readonly IErrorResponseBuilder _errorResponseBuilder;
    private readonly IMapper _mapper;

    public CommunicatieKanalenController(
        ReferentielijstenDataService dataService,
        ILogger<CommunicatieKanalenController> logger,
        IOptions<ApplicationConfiguration> options,
        IPaginationHelper paginationHelper,
        IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder
    )
    {
        _dataService = dataService;
        _logger = logger;
        _paginationHelper = paginationHelper;
        _mapper = mapper;
        _errorResponseBuilder = errorResponseBuilder;
        _configuration = options.Value;
    }

    /// <summary>
    /// Alle COMMUNICATIEKANALEN opvragen.
    /// </summary>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.CommunicatieKanalen.GetAll, Name = Operations.CommunicatieKanalen.List)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<CommunicatieKanaalResponseDto>))]
    public IActionResult GetAllAsync(int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {Page}", nameof(GetAllAsync), page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _configuration.CommunicatieKanalenPageSize));

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, _dataService.CommunicatieKanalen.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var data = _dataService
            .CommunicatieKanalen.Values.OrderBy(v => v.Url)
            .Skip(_configuration.CommunicatieKanalenPageSize * (page - 1))
            .Take(_configuration.CommunicatieKanalenPageSize);

        var result = _mapper.Map<List<CommunicatieKanaalResponseDto>>(data);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(null, pagination, result, _dataService.CommunicatieKanalen.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek COMMUNICATIEKANAAL opvragen.
    /// </summary>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.CommunicatieKanalen.Get, Name = Operations.CommunicatieKanalen.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(CommunicatieKanaalResponseDto))]
    public IActionResult GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var exists = _dataService.CommunicatieKanalen.TryGetValue(id, out var comKanal);

        if (!exists)
            return NotFound();

        return Ok(_mapper.Map<CommunicatieKanaalResponseDto>(comKanal));
    }
}
