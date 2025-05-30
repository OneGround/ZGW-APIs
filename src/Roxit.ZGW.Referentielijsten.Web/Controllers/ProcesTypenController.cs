using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Referentielijsten.Contracts.v1.Responses;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;
using Roxit.ZGW.Referentielijsten.Web.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Referentielijsten.Web.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class ProcesTypenController : ControllerBase
{
    private readonly ILogger<ProcesTypenController> _logger;
    private readonly ReferentielijstenDataService _dataService;
    private readonly IMapper _mapper;

    public ProcesTypenController(ILogger<ProcesTypenController> logger, ReferentielijstenDataService dataService, IMapper mapper)
    {
        _logger = logger;
        _dataService = dataService;
        _mapper = mapper;
    }

    /// <summary>
    /// Alle PROCESTYPEN opvragen.
    /// </summary>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ProcesTypen.GetAll, Name = Operations.ProcesTypen.List)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IList<ProcesTypeResponseDto>))]
    public IActionResult GetAllAsync([FromQuery] GetAllProcesTypenQueryParameters parameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), parameters);

        var result = _dataService.ProcesTypen.Values.Where(p => p.Jaar == parameters.Jaar || parameters.Jaar == null);

        return Ok(_mapper.Map<IList<ProcesTypeResponseDto>>(result));
    }

    /// <summary>
    /// Een specifiek PROCESTYPE opvragen.
    /// </summary>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ProcesTypen.Get, Name = Operations.ProcesTypen.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ProcesTypeResponseDto))]
    public IActionResult GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var exists = _dataService.ProcesTypen.TryGetValue(id, out var procesTypen);

        if (!exists)
            return NotFound();

        return Ok(_mapper.Map<ProcesTypeResponseDto>(procesTypen));
    }
}
