using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Expands.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Documenten.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_5)]
[Consumes("application/json")]
[Produces("application/json")]
public class GebruiksRechtenController : ZGWControllerBase
{
    private readonly IObjectExpander<InformatieObjectContext> _expander;

    public GebruiksRechtenController(
        ILogger<GebruiksRechtenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder,
        IExpanderFactory expanderFactory
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _expander = expanderFactory.Create<InformatieObjectContext>("informatieobject");
    }

    /// <summary>
    /// Alle GEBRUIKSRECHTen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(Contracts.v1.ApiRoutes.GebruiksRechten.GetAll, Name = Contracts.v1.Operations.GebruiksRechten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<GebruiksRechtResponseExpandedDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [Expand]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllGebruiksRechtenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<Models.v1.GetAllGebruiksRechtenFilter>(queryParameters);

        var result = await _mediator.Send(new Handlers.v1.GetAllGebruiksRechtenQuery { GetAllGebruiksRechtenFilter = filter });

        var gebruiksRechtenResponse = _mapper.Map<List<Documenten.Contracts.v1.Responses.GebruiksRechtResponseDto>>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var gebruiksrechtenWithOptionalExpand = gebruiksRechtenResponse
            .Select(g =>
                _expander.ResolveAsync(expandLookup, new InformatieObjectContext { InformatieObject = g.InformatieObject, ObjectDto = g }).Result
            )
            .ToList();

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = result.Result.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" },
            }
        );

        return Ok(gebruiksrechtenWithOptionalExpand);
    }

    /// <summary>
    /// Een specifieke GEBRUIKSRECHT opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(Contracts.v1.ApiRoutes.GebruiksRechten.Get, Name = Contracts.v1.Operations.GebruiksRechten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GebruiksRechtResponseExpandedDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetGebruiksRechtQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new Handlers.v1.GetGebruiksRechtQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var gebruiksrecht = _mapper.Map<Documenten.Contracts.v1.Responses.GebruiksRechtResponseDto>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var gebruiksrechtWithOptionalExpand = await _expander.ResolveAsync(
            expandLookup,
            new InformatieObjectContext { InformatieObject = gebruiksrecht.InformatieObject, ObjectDto = gebruiksrecht }
        );

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.InformatieObject,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" },
            }
        );

        return Ok(gebruiksrechtWithOptionalExpand);
    }

    /// <summary>
    /// De headers voor een specifiek(e) GEBRUIKSRECHT INFORMATIEOBJECT opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(Contracts.v1.ApiRoutes.GebruiksRechten.Get, Name = Contracts.v1.Operations.GebruiksRechten.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetGebruiksRechtQueryParameters queryParameters)
    {
        return GetAsync(id, queryParameters);
    }
}
