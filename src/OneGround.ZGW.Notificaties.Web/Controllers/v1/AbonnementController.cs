using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
public class AbonnementController : ZGWControllerBase
{
    private readonly IValidatorService _validatorService;

    public AbonnementController(
        ILogger<AbonnementController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IErrorResponseBuilder errorResponseBuilder,
        IValidatorService validatorService
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _validatorService = validatorService;
    }

    /// <summary>
    /// Alle ABONNEMENTen opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Abonnement.GetAll, Name = Operations.Abonnement.List)]
    [Scope(AuthorizationScopes.Notificaties.Consume, AuthorizationScopes.Notificaties.Produce)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<AbonnementResponseDto>))]
    public async Task<IActionResult> GetAllAsync()
    {
        _logger.LogDebug("{ControllerMethod} called", nameof(GetAllAsync));

        var result = await _mediator.Send(new GetAllAbonnementenQuery());

        var abonnementenResponse = _mapper.Map<IReadOnlyList<AbonnementResponseDto>>(result.Result);

        return Ok(abonnementenResponse);
    }

    /// <summary>
    /// Een specifiek ABONNEMENT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Abonnement.Get, Name = Operations.Abonnement.Read)]
    [Scope(AuthorizationScopes.Notificaties.Consume, AuthorizationScopes.Notificaties.Produce)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AbonnementResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetAbonnementQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var abonnementResponse = _mapper.Map<AbonnementResponseDto>(result.Result);

        return Ok(abonnementResponse);
    }

    /// <summary>
    /// Maak een ABONNEMENT aan
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Abonnement.Create, Name = Operations.Abonnement.Create)]
    [Scope(AuthorizationScopes.Notificaties.Consume)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(AbonnementResponseDto))]
    public async Task<IActionResult> CreateAsync([FromBody] AbonnementRequestDto abonnementRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(CreateAsync), abonnementRequest);

        Abonnement abonnement = _mapper.Map<Abonnement>(abonnementRequest);

        var result = await _mediator.Send(new CreateAbonnementCommand { Abonnement = abonnement });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var abonnementResponse = _mapper.Map<AbonnementResponseDto>(result.Result);

        return Created(abonnementResponse.Url, abonnementResponse);
    }

    /// <summary>
    /// Werk een ABONNEMENT in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.Abonnement.Update, Name = Operations.Abonnement.Update)]
    [Scope(AuthorizationScopes.Notificaties.Consume)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AbonnementResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] AbonnementRequestDto abonnementRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), abonnementRequest, id);

        Abonnement abonnement = _mapper.Map<Abonnement>(abonnementRequest);

        var result = await _mediator.Send(new UpdateAbonnementCommand { Abonnement = abonnement, Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var abonnementResponse = _mapper.Map<AbonnementResponseDto>(result.Result);

        return Ok(abonnementResponse);
    }

    /// <summary>
    /// Werk een ABONNEMENT deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.Abonnement.Update, Name = Operations.Abonnement.PartialUpdate)]
    [Scope(AuthorizationScopes.Notificaties.Consume)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AbonnementResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialAbonnementRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetAbonnementQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        AbonnementRequestDto mergedAbonnementRequest = _requestMerger.MergePartialUpdateToObjectRequest<AbonnementRequestDto, Abonnement>(
            resultGet.Result,
            partialAbonnementRequest
        );

        if (!_validatorService.IsValid(mergedAbonnementRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        Abonnement mergedAbonnement = _mapper.Map<Abonnement>(mergedAbonnementRequest);

        var resultUpd = await _mediator.Send(new UpdateAbonnementCommand { Abonnement = mergedAbonnement, Id = id });

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var abonnementResponse = _mapper.Map<AbonnementResponseDto>(resultUpd.Result);

        return Ok(abonnementResponse);
    }

    /// <summary>
    /// Verwijder een ABONNEMENT.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.Abonnement.Delete, Name = Operations.Abonnement.Delete)]
    [Scope(AuthorizationScopes.Notificaties.Consume)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteAbonnementCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        return NoContent();
    }
}
