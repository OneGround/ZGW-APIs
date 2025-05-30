using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Contracts.v1._5.Queries;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Contracts.v1._5;
using Roxit.ZGW.Zaken.Web.Handlers.v1._5;
using Roxit.ZGW.Zaken.Web.Models.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Zaken.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_5)]
public class ZaakVerzoekenController : ZGWControllerBase
{
    public ZaakVerzoekenController(
        ILogger<ZaakVerzoekenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Alle ZAAK-VERZOEKen opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakVerzoeken.GetAll, Name = Operations.ZaakVerzoeken.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IList<ZaakVerzoekResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakVerzoekenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<GetAllZaakVerzoekenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakVerzoekenQuery { GetAllZaakVerzoekenFilter = filter });

        var zaakVerzoekResponse = _mapper.Map<IList<ZaakVerzoekResponseDto>>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = zaakVerzoekResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakverzoek" },
            }
        );

        return Ok(zaakVerzoekResponse);
    }

    /// <summary>
    /// Een specifieke ZAAK-VERZOEK opvragen.
    /// </summary>
    /// <param name="id">Unieke resource identifier (UUID4)</param>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakVerzoeken.Get, Name = Operations.ZaakVerzoeken.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakVerzoekResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakVerzoekQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakVerzoekResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakverzoek" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een ZAAK-VERZOEK aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakVerzoeken.Create, Name = Operations.ZaakVerzoeken.Create)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakVerzoekResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakVerzoekRequestDto zaakVerzoekRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakVerzoekRequest);

        ZaakVerzoek zaakverzoek = _mapper.Map<ZaakVerzoek>(zaakVerzoekRequest);

        var result = await _mediator.Send(new CreateZaakVerzoekCommand { ZaakVerzoek = zaakverzoek, ZaakUrl = zaakVerzoekRequest.Zaak });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakVerzoekResponse = _mapper.Map<ZaakVerzoekResponseDto>(result.Result);

        return Created(zaakVerzoekResponse.Url, zaakVerzoekResponse);
    }

    /// <summary>
    /// Verwijder een ZAAK-VERZOEK.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakVerzoeken.Delete, Name = Operations.ZaakVerzoeken.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakVerzoekCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        return NoContent();
    }
}
