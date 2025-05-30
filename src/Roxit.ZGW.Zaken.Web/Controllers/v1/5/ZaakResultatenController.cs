using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Filters;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Contracts.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Zaken.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_5)]
public class ZaakResultatenController : ZGWControllerBase
{
    public ZaakResultatenController(
        ILogger<ZaakResultatenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Een specifieke RESULTAAT opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakResultaten.Get, Name = Operations.ZaakResultaten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new Handlers.v1.GetZaakResultaatQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<Zaken.Contracts.v1.Responses.ZaakResultaatResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "resultaat" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifiek(e) RESULTAAT opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakResultaten.Get, Name = Operations.ZaakResultaten.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
    }
}
