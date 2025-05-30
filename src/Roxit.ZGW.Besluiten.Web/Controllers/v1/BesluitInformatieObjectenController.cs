using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.Contracts.v1.Requests;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.Authorization;
using Roxit.ZGW.Besluiten.Web.Contracts.v1;
using Roxit.ZGW.Besluiten.Web.Handlers.v1;
using Roxit.ZGW.Besluiten.Web.Models.v1;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Besluiten.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class BesluitInformatieObjectenController : ZGWControllerBase
{
    public BesluitInformatieObjectenController(
        ILogger<BesluitInformatieObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Alle BESLUIT-INFORMATIEOBJECT relaties opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.BesluitInformatieObjecten.GetAll, Name = Operations.BesluitInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Besluiten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<BesluitInformatieObjectResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllBesluitInformatieObjectenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<GetAllBesluitInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllBesluitInformatieObjectenQuery { GetAllBesluitInformatieObjectenFilter = filter });

        var besluitInformatieObjectenResponse = _mapper.Map<List<BesluitInformatieObjectResponseDto>>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = besluitInformatieObjectenResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluitinformatieobject" },
            }
        );

        return Ok(besluitInformatieObjectenResponse);
    }

    /// <summary>
    /// Een specifieke BESLUIT-INFORMATIEOBJECT relatie opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.BesluitInformatieObjecten.Get, Name = Operations.BesluitInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Besluiten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitInformatieObjectResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetBesluitInformatieObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var besluitInformatieObjectResponse = _mapper.Map<BesluitInformatieObjectResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Besluit,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluitinformatieobject" },
            }
        );

        return Ok(besluitInformatieObjectResponse);
    }

    /// <summary>
    /// Maak een BESLUIT-INFORMATIEOBJECT relatie aan.
    /// Registreer een INFORMATIEOBJECT bij een BESLUIT. Er worden twee types van relaties met andere objecten gerealiseerd
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.BesluitInformatieObjecten.Create, Name = Operations.BesluitInformatieObjecten.Create)]
    [Scope(AuthorizationScopes.Besluiten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(BesluitInformatieObjectResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] BesluitInformatieObjectRequestDto besluitInformatieObjectRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), besluitInformatieObjectRequest);

        BesluitInformatieObject besluitInformatieObject = _mapper.Map<BesluitInformatieObject>(besluitInformatieObjectRequest);

        var result = await _mediator.Send(
            new CreateBesluitInformatieObjectCommand
            {
                BesluitInformatieObject = besluitInformatieObject,
                BesluitUrl = besluitInformatieObjectRequest.Besluit,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var besluitResponse = _mapper.Map<BesluitInformatieObjectResponseDto>(result.Result);

        return Created(besluitResponse.Url, besluitResponse);
    }

    /// <summary>
    /// Verwijder een BESLUIT-INFORMATIEOBJECT relatie.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.BesluitInformatieObjecten.Delete, Name = Operations.BesluitInformatieObjecten.Delete)]
    [Scope(AuthorizationScopes.Besluiten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteBesluitInformatieObjectCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        return NoContent();
    }
}
