using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Besluiten.Web.Contracts.v1;
using OneGround.ZGW.Besluiten.Web.Handlers.v1;
using OneGround.ZGW.Besluiten.Web.Models.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Besluiten.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class BesluitInformatieObjectenController : ZGWControllerBase
{
    private readonly MapsterMapper.IMapper _mapsterMapper;

    public BesluitInformatieObjectenController(
        ILogger<BesluitInformatieObjectenController> logger,
        IMediator mediator,
        AutoMapper.IMapper mapper,
        MapsterMapper.IMapper mapsterMapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _mapsterMapper = mapsterMapper;
    }

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
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllBesluitInformatieObjectenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllBesluitInformatieObjectenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapsterMapper.Map<GetAllBesluitInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllBesluitInformatieObjectenQuery { GetAllBesluitInformatieObjectenFilter = filter });

        var besluitInformatieObjectenResponse = _mapsterMapper.Map<List<BesluitInformatieObjectResponseDto>>(result.Result);

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

        var besluitInformatieObjectResponse = _mapsterMapper.Map<BesluitInformatieObjectResponseDto>(result.Result);

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

        BesluitInformatieObject besluitInformatieObject = _mapsterMapper.Map<BesluitInformatieObject>(besluitInformatieObjectRequest);

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

        var besluitResponse = _mapsterMapper.Map<BesluitInformatieObjectResponseDto>(result.Result);

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
