using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Contracts.v1;
using OneGround.ZGW.Documenten.Web.Handlers.v1;
using OneGround.ZGW.Documenten.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Documenten.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
public class GebruiksRechtenController : ZGWControllerBase
{
    private readonly IValidatorService _validatorService;

    public GebruiksRechtenController(
        ILogger<GebruiksRechtenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder) { }

    /// <summary>
    /// Alle GEBRUIKSRECHTen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.GebruiksRechten.GetAll, Name = Operations.GebruiksRechten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<GebruiksRechtResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] GetAllGebruiksRechtenQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<GetAllGebruiksRechtenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllGebruiksRechtenQuery { GetAllGebruiksRechtenFilter = filter }, cancellationToken);

        var gebruiksRechtenResponse = _mapper.Map<List<GebruiksRechtResponseDto>>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = result.Result.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" },
            },
            cancellationToken
        );

        return Ok(gebruiksRechtenResponse);
    }

    /// <summary>
    /// Een specifieke GEBRUIKSRECHT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.GebruiksRechten.Get, Name = Operations.GebruiksRechten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GebruiksRechtResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetGebruiksRechtQuery { Id = id }, cancellationToken);

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var gebruiksRechtResponse = _mapper.Map<GebruiksRechtResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.InformatieObject,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" },
            },
            cancellationToken
        );

        return Ok(gebruiksRechtResponse);
    }

    /// <summary>
    /// Voeg GEBRUIKSRECHTen toe voor een INFORMATIEOBJECT.
    /// </summary>
    /// <remarks>
    /// Het toevoegen van gebruiksrechten zorgt ervoor dat de indicatieGebruiksrecht op het informatieobject op true gezet wordt.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.GebruiksRechten.Create, Name = Operations.GebruiksRechten.Create)]
    [Scope(AuthorizationScopes.Documenten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(GebruiksRechtResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> AddAsync([FromBody] GebruiksRechtRequestDto gebruiksRechtRequest, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), gebruiksRechtRequest);

        GebruiksRecht gebruiksRecht = _mapper.Map<GebruiksRecht>(gebruiksRechtRequest);

        var result = await _mediator.Send(
            new CreateGebruiksRechtCommand { GebruiksRecht = gebruiksRecht, InformatieObjectUrl = gebruiksRechtRequest.InformatieObject },
            cancellationToken
        );

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var objectInformatieObjectResponse = _mapper.Map<GebruiksRechtResponseDto>(result.Result);

        return Created(objectInformatieObjectResponse.Url, objectInformatieObjectResponse);
    }

    /// <summary>
    /// Werk een GEBRUIKSRECHT in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.GebruiksRechten.Update, Name = Operations.GebruiksRechten.Update)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GebruiksRechtResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] GebruiksRechtRequestDto gebruiksRechtRequest,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), gebruiksRechtRequest, id);

        GebruiksRecht gebruiksRecht = _mapper.Map<GebruiksRecht>(gebruiksRechtRequest);

        var result = await _mediator.Send(
            new UpdateGebruiksRechtCommand
            {
                Id = id,
                InformatieObjectUrl = gebruiksRechtRequest.InformatieObject,
                GebruiksRecht = gebruiksRecht, // Note: Indicates that the versie should be fully replaced in the command handler
                PartialObject = null,
            },
            cancellationToken
        );

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var gebruiksRechtResponse = _mapper.Map<GebruiksRechtResponseDto>(result.Result);

        return Ok(gebruiksRechtResponse);
    }

    /// <summary>
    /// Werk een GEBRUIKSRECHT relatie deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.GebruiksRechten.Update, Name = Operations.GebruiksRechten.PartialUpdate)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(GebruiksRechtResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialGebruiksRechtRequest, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var result = await _mediator.Send(
            new UpdateGebruiksRechtCommand
            {
                Id = id,
                InformatieObjectUrl = GetValueFromPartial<string>(partialGebruiksRechtRequest, "informatieObject"),
                GebruiksRecht = null,
                PartialObject = partialGebruiksRechtRequest, // Note: Indicates that the versie should be merged in the command handler
            },
            cancellationToken
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var gebruiksRechtResponse = _mapper.Map<GebruiksRechtResponseDto>(result.Result);

        return Ok(gebruiksRechtResponse);
    }

    /// <summary>
    /// Verwijder een GEBRUIKSRECHT.
    /// </summary>
    /// <remarks>
    /// Indien het laatste GEBRUIKSRECHT van een INFORMATIEOBJECT verwijderd wordt, dan wordt de indicatieGebruiksrecht van het INFORMATIEOBJECT op null gezet.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.GebruiksRechten.Delete, Name = Operations.GebruiksRechten.Delete)]
    [Scope(AuthorizationScopes.Documenten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteGebruiksRechtCommand { Id = id }, cancellationToken);

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        return NoContent();
    }
}
