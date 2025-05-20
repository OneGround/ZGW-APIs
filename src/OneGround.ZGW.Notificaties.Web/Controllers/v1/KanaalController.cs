using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
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
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.Authorization;
using OneGround.ZGW.Notificaties.Web.Handlers;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Notificaties.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class KanaalController : ZGWControllerBase
{
    public KanaalController(
        ILogger<KanaalController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Alle KANAALen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameter.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Kanaal.GetAll, Name = Operations.Kanaal.List)]
    [Scope(AuthorizationScopes.Notificaties.Produce, AuthorizationScopes.Notificaties.Consume)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<KanaalResponseDto>))]
    public async Task<IActionResult> GetAllAsync(string naam)
    {
        _logger.LogDebug("{ControllerMethod} called", nameof(GetAllAsync));

        var result = await _mediator.Send(new GetAllKanalenQuery(naam));

        var kanalenResponse = _mapper.Map<IReadOnlyList<KanaalResponseDto>>(result.Result);

        return Ok(kanalenResponse);
    }

    /// <summary>
    /// Een specifiek KANAAL opvragen
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Kanaal.Get, Name = Operations.Kanaal.Read)]
    [Scope(AuthorizationScopes.Notificaties.Produce, AuthorizationScopes.Notificaties.Consume)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(KanaalResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetKanaalQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var kanaalResponse = _mapper.Map<KanaalResponseDto>(result.Result);

        return Ok(kanaalResponse);
    }

    /// <summary>
    /// Maak een KANAAL aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Kanaal.Create, Name = Operations.Kanaal.Create)]
    [Scope(AuthorizationScopes.Notificaties.Produce)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(KanaalResponseDto))]
    public async Task<IActionResult> CreateAsync([FromBody] KanaalRequestDto kanaalRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(CreateAsync), kanaalRequest);

        Kanaal kanaal = _mapper.Map<Kanaal>(kanaalRequest);

        var result = await _mediator.Send(new CreateKanaalCommand { Kanaal = kanaal });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var kanaalResponse = _mapper.Map<KanaalResponseDto>(result.Result);

        return Created(kanaalResponse.Url, kanaalResponse);
    }
}
