using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Referentielijsten.Contracts.v1.Responses;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1;
using Roxit.ZGW.Referentielijsten.Web.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Referentielijsten.Web.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class ResultaatTypeomschrijvingenController : ControllerBase
{
    private readonly ILogger<ResultaatTypeomschrijvingenController> _logger;
    private readonly ReferentielijstenDataService _dataService;
    private readonly IMapper _mapper;

    public ResultaatTypeomschrijvingenController(
        ILogger<ResultaatTypeomschrijvingenController> logger,
        ReferentielijstenDataService dataService,
        IMapper mapper
    )
    {
        _logger = logger;
        _dataService = dataService;
        _mapper = mapper;
    }

    /// <summary>
    /// Alle RESULTAATTYPEOMSCHRIJVINGEN opvragen.
    /// </summary>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ResultaatTypeomschrijvingen.GetAll, Name = Operations.ResultaatTypeomschrijvingen.List)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IList<ResultaatTypeOmschrijvingResponseDto>))]
    public IActionResult GetAllAsync()
    {
        _logger.LogDebug("{ControllerMethod} called", nameof(GetAllAsync));

        var data = _dataService.ResultaatTypeOmschrijvingen.Values;
        return Ok(_mapper.Map<IList<ResultaatTypeOmschrijvingResponseDto>>(data));
    }

    /// <summary>
    /// Een specifiek RESULTAATTYPEOMSCHRIJVING opvragen.
    /// </summary>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ResultaatTypeomschrijvingen.Get, Name = Operations.ResultaatTypeomschrijvingen.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ResultaatTypeOmschrijvingResponseDto))]
    public IActionResult GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var exists = _dataService.ResultaatTypeOmschrijvingen.TryGetValue(id, out var resultaatTypeOmschrijving);

        if (!exists)
            return NotFound();

        return Ok(_mapper.Map<ResultaatTypeOmschrijvingResponseDto>(resultaatTypeOmschrijving));
    }
}
