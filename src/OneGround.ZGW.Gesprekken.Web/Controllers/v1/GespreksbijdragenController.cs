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
using OneGround.ZGW.Gesprekken.Web.Helpers;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Gesprekken.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ProblemJson]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class GespreksbijdragenController : ZGWControllerBase
{
    public GespreksbijdragenController(
        ILogger<GespreksbijdragenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, errorResponseBuilder) { }

    /// <summary>
    /// Een GESPREKSBIJDRAGE aan een bestaand gesprek toevoegen.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gespreksbijdragen.Create, Name = Operations.Gespreksbijdragen.Create)]
    [Scope(AuthorizationScopes.Gesprekken.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(GespreksbijdrageResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] GespreksbijdrageRequestDto request)
    {
        var command = new CreateGespreksbijdrageCommand
        {
            Bijdrage = _mapper.Map<Gespreksbijdrage>(request),
            VerzenderId = GrcUrl.GetIdOrNull(request.Verzender.Gespreksdeelname, GrcUrl.Gespreksdeelnames),
            NieuweVerzender = request.Verzender.NieuweDeelname == null ? null : _mapper.Map<Gespreksdeelname>(request.Verzender.NieuweDeelname),
        };

        var result = await _mediator.Send(command);

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<GespreksbijdrageResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Registreren dat een gespreksdeelname deze GESPREKSBIJDRAGE heeft gelezen. Opnieuw registreren geeft de bestaande registratie terug.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gespreksbijdragen.Gelezen, Name = Operations.Gespreksbijdragen.Gelezen)]
    [Scope(AuthorizationScopes.Gesprekken.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(GelezenGespreksbijdrageResponseDto))]
    public async Task<IActionResult> GelezenAsync([FromBody] GelezenGespreksbijdrageRequestDto request, Guid id)
    {
        var result = await _mediator.Send(
            new MarkeerGelezenCommand
            {
                GespreksbijdrageId = id,
                GespreksdeelnameId = GrcUrl.GetIdOrNull(request.Gespreksdeelname, GrcUrl.Gespreksdeelnames) ?? Guid.Empty,
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

        var response = _mapper.Map<GelezenGespreksbijdrageResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Een GESPREKSBIJDRAGE redigeren (beheer): de bijdrage verdwijnt, ook uit de eventhistorie.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gespreksbijdragen.Redigeren, Name = Operations.Gespreksbijdragen.Redigeren)]
    [Scope(AuthorizationScopes.Gesprekken.Redigeren)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(RedactieResponseDto))]
    public async Task<IActionResult> RedigerenAsync([FromBody] RedactieRequestDto request, Guid id)
    {
        var result = await _mediator.Send(new RedigeerGespreksbijdrageCommand { Id = id, Reden = request.Reden });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(result.Result);
    }
}
