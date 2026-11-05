using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;
using OneGround.ZGW.Gesprekken.Contracts.v1.Responses;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Authorization;
using OneGround.ZGW.Gesprekken.Web.Contracts.v1;
using OneGround.ZGW.Gesprekken.Web.Handlers.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Gesprekken.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ProblemJson]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class GespreksdeelnamesController : ZGWControllerBase
{
    public GespreksdeelnamesController(
        ILogger<GespreksdeelnamesController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, errorResponseBuilder) { }

    /// <summary>
    /// Een GESPREKSDEELNAME aan een bestaand gesprek toevoegen.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gespreksdeelnames.Create, Name = Operations.Gespreksdeelnames.Create)]
    [Scope(AuthorizationScopes.Gesprekken.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(GespreksdeelnameResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] GespreksdeelnameRequestDto request)
    {
        var result = await _mediator.Send(new CreateGespreksdeelnameCommand { Deelname = _mapper.Map<Gespreksdeelname>(request) });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<GespreksdeelnameResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Een GESPREKSDEELNAME beëindigen: zet momentEindeDeelname op het huidige moment.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gespreksdeelnames.Eindigen, Name = Operations.Gespreksdeelnames.Eindigen)]
    [Scope(AuthorizationScopes.Gesprekken.Update)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GespreksdeelnameResponseDto))]
    public async Task<IActionResult> EindigenAsync(Guid id)
    {
        var result = await _mediator.Send(new EindigGespreksdeelnameCommand { Id = id });

        return result.Status switch
        {
            CommandStatus.NotFound => _errorResponseBuilder.NotFound(),
            CommandStatus.ValidationError => _errorResponseBuilder.BadRequest(result.Errors),
            _ => Ok(_mapper.Map<GespreksdeelnameResponseDto>(result.Result)),
        };
    }
}
