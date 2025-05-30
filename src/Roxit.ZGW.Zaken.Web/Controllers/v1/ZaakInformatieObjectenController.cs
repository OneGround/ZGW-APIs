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
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Contracts.v1;
using Roxit.ZGW.Zaken.Web.Handlers.v1;
using Roxit.ZGW.Zaken.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Zaken.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
[ZgwApiVersion(Api.LatestVersion_1_2)]
public class ZaakInformatieObjectenController : ZGWControllerBase
{
    private readonly IValidatorService _validatorService;

    public ZaakInformatieObjectenController(
        ILogger<ZaakInformatieObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _validatorService = validatorService;
    }

    /// <summary>
    /// Alle ZAAK-INFORMATIEOBJECT relaties opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakInformatieObjecten.GetAll, Name = Operations.ZaakInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IList<ZaakInformatieObjectResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakInformatieObjectenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var filter = _mapper.Map<GetAllZaakInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakInformatieObjectenQuery { GetAllZaakInformatieObjectenFilter = filter });

        if (result.Status == QueryStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var zaakInformatieObjectenResponse = _mapper.Map<IList<ZaakInformatieObjectResponseDto>>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = zaakInformatieObjectenResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" },
            }
        );

        return Ok(zaakInformatieObjectenResponse);
    }

    /// <summary>
    /// Een specifieke ZAAK-INFORMATIEOBJECT relatie opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakInformatieObjecten.Get, Name = Operations.ZaakInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakInformatieObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakInformatieObjectResponse = _mapper.Map<ZaakInformatieObjectResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" },
            }
        );

        return Ok(zaakInformatieObjectResponse);
    }

    /// <summary>
    /// Maak een ZAAK-INFORMATIEOBJECT relatie aan.
    /// Er worden twee types van relaties met andere objecten gerealiseerd: ZaakUrl en informatieobject URL
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakInformatieObjecten.Create, Name = Operations.ZaakInformatieObjecten.Create)]
    [Scope(AuthorizationScopes.Zaken.Create, AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakInformatieObjectRequestDto zaakInformatieObjectRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakInformatieObjectRequest);

        ZaakInformatieObject zaakInformatieObject = _mapper.Map<ZaakInformatieObject>(zaakInformatieObjectRequest);

        var result = await _mediator.Send(
            new CreateZaakInformatieObjectCommand { ZaakInformatieObject = zaakInformatieObject, ZaakUrl = zaakInformatieObjectRequest.Zaak }
        );

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

        var zaakResponse = _mapper.Map<ZaakInformatieObjectResponseDto>(result.Result);

        return Created(zaakResponse.Url, zaakResponse);
    }

    /// <summary>
    /// Werk een ZAAK-INFORMATIEOBJECT relatie in zijn geheel bij. Je mag enkel de gegevens van de relatie bewerken, en niet de relatie zelf aanpassen
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakInformatieObjecten.Update, Name = Operations.ZaakInformatieObjecten.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakInformatieObjectRequestDto zaakInformatieObjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakInformatieObjectRequest, id);

        ZaakInformatieObject zaakInformatieObject = _mapper.Map<ZaakInformatieObject>(zaakInformatieObjectRequest);

        var result = await _mediator.Send(
            new UpdateZaakInformatieObjectCommand
            {
                ZaakInformatieObject = zaakInformatieObject,
                Id = id,
                ZaakUrl = zaakInformatieObjectRequest.Zaak,
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

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakInformatieObjectResponse = _mapper.Map<ZaakInformatieObjectResponseDto>(result.Result);

        return Ok(zaakInformatieObjectResponse);
    }

    /// <summary>
    /// Werk een ZAAK-INFORMATIEOBJECT relatie in deels bij. Je mag enkel de gegevens van de relatie bewerken, en niet de relatie zelf aanpassen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakInformatieObjecten.Update, Name = Operations.ZaakInformatieObjecten.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakInformatieObjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakInformatieObjectQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        ZaakInformatieObjectRequestDto mergedZaakInformatieObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            ZaakInformatieObjectRequestDto,
            ZaakInformatieObject
        >(resultGet.Result, partialZaakInformatieObjectRequest);

        if (!_validatorService.IsValid(mergedZaakInformatieObjectRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakInformatieObject mergedZaakInformatieObject = _mapper.Map<ZaakInformatieObject>(mergedZaakInformatieObjectRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakInformatieObjectCommand
            {
                ZaakInformatieObject = mergedZaakInformatieObject,
                Id = id,
                ZaakUrl = mergedZaakInformatieObjectRequest.Zaak,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        if (resultUpd.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakInformatieObjectResponse = _mapper.Map<ZaakInformatieObjectResponseDto>(resultUpd.Result);

        return Ok(zaakInformatieObjectResponse);
    }

    /// <summary>
    /// Verwijder een ZAAK-INFORMATIEOBJECT relatie.
    /// </summary>
    /// <remarks>
    /// De gespiegelde relatie in de Documenten API wordt door de Zaken API verwijderd. Consumers kunnen dit niet handmatig doen.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakInformatieObjecten.Delete, Name = Operations.ZaakInformatieObjecten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate, AuthorizationScopes.Zaken.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakInformatieObjectCommand { Id = id });

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
