using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Gesprekken.Contracts.v1.Queries;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;
using OneGround.ZGW.Gesprekken.Contracts.v1.Responses;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Authorization;
using OneGround.ZGW.Gesprekken.Web.Configuration;
using OneGround.ZGW.Gesprekken.Web.Contracts.v1;
using OneGround.ZGW.Gesprekken.Web.Events;
using OneGround.ZGW.Gesprekken.Web.Handlers.v1;
using OneGround.ZGW.Gesprekken.Web.Models.v1;
using OneGround.ZGW.Gesprekken.Web.Paginering;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Gesprekken.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ProblemJson]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class GesprekkenController : ZGWControllerBase
{
    // patterns/pagination: default 20, at most 100 per page
    private const int DefaultPageSize = 20;

    private readonly IGesprekEventReader _eventReader;
    private readonly IAuthorizationContextAccessor _authorizationContextAccessor;

    public GesprekkenController(
        ILogger<GesprekkenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IErrorResponseBuilder errorResponseBuilder,
        IGesprekEventReader eventReader,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _eventReader = eventReader;
        _authorizationContextAccessor = authorizationContextAccessor;
    }

    /// <summary>
    /// Alle GESPREKken opvragen, nieuwste eerst, met cursor-paginering: volg next tot die null is.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Gesprekken.GetAll, Name = Operations.Gesprekken.List)]
    [Scope(AuthorizationScopes.Gesprekken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(CursorPagedResponseDto<GesprekInLijstResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllGesprekkenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllGesprekkenQueryParameters queryParameters)
    {
        if (!TryReadCursor(queryParameters.Cursor, out var after, out var cursorError))
        {
            return cursorError;
        }

        await SetFeedCursorAsync();

        var filter = _mapper.Map<GetAllGesprekkenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllGesprekkenQuery
            {
                Filter = filter,
                After = after,
                PageSize = PageSize(queryParameters.PageSize),
            }
        );

        return Ok(ToResponse(result.Result, queryParameters));
    }

    private static int PageSize(string pageSize) => pageSize is null ? DefaultPageSize : int.Parse(pageSize);

    private bool TryReadCursor(string value, out GesprekCursor cursor, out IActionResult error)
    {
        cursor = null;
        error = null;
        if (value == null)
        {
            return true;
        }

        if (GesprekCursor.TryDecode(value, out cursor))
        {
            return true;
        }

        error = _errorResponseBuilder.BadRequest([
            new ValidationError("cursor", ErrorCode.Invalid, "Ongeldige cursor; gebruik de next-link van een eerdere pagina."),
        ]);
        return false;
    }

    private CursorPagedResponseDto<GesprekInLijstResponseDto> ToResponse(CursorPage<Gesprek> page, IQueryParameters queryParameters) =>
        new()
        {
            Next = GesprekPaginering.NextLink(Request, queryParameters, page.Next),
            Count = page.Count,
            Results = _mapper.Map<List<GesprekInLijstResponseDto>>(page.Results),
        };

    /// <summary>
    /// Een specifiek GESPREK opvragen, met de gespreksbijdragen (oudste eerst).
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Gesprekken.Get, Name = Operations.Gesprekken.Read)]
    [Scope(AuthorizationScopes.Gesprekken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GesprekDetailResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        await SetFeedCursorAsync();

        var result = await _mediator.Send(new GetGesprekQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        return Ok(_mapper.Map<GesprekDetailResponseDto>(result.Result));
    }

    /// <summary>
    /// The position in the event stream, determined before the snapshot is read: following GET /events from here
    /// misses no change made during or after reading.
    /// </summary>
    private async Task SetFeedCursorAsync()
    {
        var rsin = _authorizationContextAccessor.AuthorizationContext.Authorization.Rsin;
        Response.Headers["Feed-Cursor"] = (await _eventReader.GetHeadAsync(rsin, HttpContext.RequestAborted)).ToString();
    }

    /// <summary>
    /// Een GESPREK aanmaken, samen met de ontvanger(s) en de eerste gespreksbijdrage.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gesprekken.Create, Name = Operations.Gesprekken.Create)]
    [Scope(AuthorizationScopes.Gesprekken.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(GesprekResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] GesprekRequestDto gesprekRequest)
    {
        var command = new CreateGesprekCommand
        {
            Gesprek = _mapper.Map<Gesprek>(gesprekRequest),
            Ontvangers = gesprekRequest.Ontvanger.Select(o => _mapper.Map<Gespreksdeelname>(o)).ToList(),
            Verzender = _mapper.Map<Gespreksdeelname>(gesprekRequest.EersteBijdrage.Verzender.NieuweDeelname),
            EersteBijdrage = _mapper.Map<Gespreksbijdrage>(gesprekRequest.EersteBijdrage),
        };

        var result = await _mediator.Send(command);

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<GesprekResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Een GESPREK beëindigen: zet momentEindeGesprek op het huidige moment.
    /// </summary>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Gesprekken.Eindigen, Name = Operations.Gesprekken.Eindigen)]
    [Scope(AuthorizationScopes.Gesprekken.Update)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GesprekResponseDto))]
    public async Task<IActionResult> EindigenAsync(Guid id)
    {
        var result = await _mediator.Send(new EindigGesprekCommand { Id = id });

        return result.Status switch
        {
            CommandStatus.NotFound => _errorResponseBuilder.NotFound(),
            CommandStatus.ValidationError => _errorResponseBuilder.BadRequest(result.Errors),
            _ => Ok(_mapper.Map<GesprekResponseDto>(result.Result)),
        };
    }
}
