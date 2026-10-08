using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

// Note: Only GetAllAsync/GetAsync/HeadAsync use the new (v1.7) ExpandEngine and the new
// v1._7.Responses.ZaakInformatieObjectResponseDto (which implements IExpandable). Add/Update/
// PartialUpdate/Delete's payload does not change for 1.7, so they keep using the v1._5 Request/
// Response DTOs and the v1._5 MediatR commands they already send -- same reasoning as
// Controllers/v1/7/ZaakObjectenController.cs. There is no "fields" mechanism here -- unlike Zaken's
// /_zoek, the ZRC 1.7.0 spec only adds "expand" for zaakinformatieobjecten.
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaakInformatieObjectenController : ZGWControllerBase
{
    private readonly IValidatorService _validatorService;
    private readonly IRequestMerger _requestMerger;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ZaakInformatieObjectResponseDto> _expandValidator;
    private readonly ExpandEngine<ZaakInformatieObjectResponseDto> _expandEngine;

    public ZaakInformatieObjectenController(
        ILogger<ZaakInformatieObjectenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<ZaakInformatieObjectResponseDto> expandValidator,
        ExpandEngine<ZaakInformatieObjectResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _requestMerger = requestMerger;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _validatorService = validatorService;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
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
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.GetAll, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IList<ZaakInformatieObjectResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakInformatieObjectenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakInformatieObjectenQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetAllAsync), queryParameters);

        var expandValidationResult = ValidateExpand(
            _expandValidator,
            queryParameters.Expand,
            _applicationConfiguration.ExpandSettings.List,
            out var expandPaths
        );
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var filter = _mapper.Map<Models.v1.GetAllZaakInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new Handlers.v1._5.GetAllZaakInformatieObjectenQuery { GetAllZaakInformatieObjectenFilter = filter });

        if (result.Status == QueryStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<List<ZaakInformatieObjectResponseDto>>(result.Result);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(response, expandPaths);
            }
            catch (ExpandExternalServiceException ex)
            {
                return ExterneServiceFout(ex.ServiceName, ex.ServiceUrl, ex.StatusCode, ex);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = response.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Een specifieke ZAAK-INFORMATIEOBJECT relatie opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.Get, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakInformatieObjectResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakInformatieObjectQueryParameters>))]
    public async Task<IActionResult> GetAsync([FromQuery] GetZaakInformatieObjectQueryParameters queryParameters, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var expandValidationResult = ValidateExpand(
            _expandValidator,
            queryParameters.Expand,
            _applicationConfiguration.ExpandSettings.Get,
            out var expandPaths
        );
        if (expandValidationResult is not null)
        {
            return expandValidationResult;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakInformatieObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakInformatieObjectResponseDto>(result.Result);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(response, expandPaths);
            }
            catch (ExpandExternalServiceException ex)
            {
                return ExterneServiceFout(ex.ServiceName, ex.ServiceUrl, ex.StatusCode, ex);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ZAAKINFORMATIEOBJECT opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.Get, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakInformatieObjectQueryParameters>))]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetZaakInformatieObjectQueryParameters queryParameters)
    {
        return GetAsync(queryParameters, id);
    }

    /// <summary>
    /// Maak een ZAAK-INFORMATIEOBJECT relatie aan.
    /// Er worden twee types van relaties met andere objecten gerealiseerd: ZaakUrl en informatieobject URL
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.Create, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.Create)]
    [Scope(AuthorizationScopes.Zaken.Create, AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakInformatieObjectRequestDto zaakInformatieObjectRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakInformatieObjectRequest);

        ZaakInformatieObject zaakInformatieObject = _mapper.Map<ZaakInformatieObject>(zaakInformatieObjectRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.CreateZaakInformatieObjectCommand
            {
                ZaakInformatieObject = zaakInformatieObject,
                ZaakUrl = zaakInformatieObjectRequest.Zaak,
                StatusUrl = zaakInformatieObjectRequest.Status,
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

        var zaakResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakInformatieObjectResponseDto>(result.Result);

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
    [HttpPut(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.Update, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] Zaken.Contracts.v1._5.Requests.ZaakInformatieObjectRequestDto zaakInformatieObjectRequest,
        Guid id
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakInformatieObjectRequest, id);

        ZaakInformatieObject zaakInformatieObject = _mapper.Map<ZaakInformatieObject>(zaakInformatieObjectRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.UpdateZaakInformatieObjectCommand
            {
                ZaakInformatieObject = zaakInformatieObject,
                Id = id,
                ZaakUrl = zaakInformatieObjectRequest.Zaak,
                StatusUrl = zaakInformatieObjectRequest.Status,
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

        var zaakInformatieObjectResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakInformatieObjectResponseDto>(result.Result);

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
    [HttpPatch(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.Update, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakInformatieObjectResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakInformatieObjectRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new Handlers.v1._5.GetZaakInformatieObjectQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        Zaken.Contracts.v1._5.Requests.ZaakInformatieObjectRequestDto mergedZaakInformatieObjectRequest =
            _requestMerger.MergePartialUpdateToObjectRequest<Zaken.Contracts.v1._5.Requests.ZaakInformatieObjectRequestDto, ZaakInformatieObject>(
                resultGet.Result,
                partialZaakInformatieObjectRequest
            );

        if (!_validatorService.IsValid(mergedZaakInformatieObjectRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakInformatieObject mergedZaakInformatieObject = _mapper.Map<ZaakInformatieObject>(mergedZaakInformatieObjectRequest);

        var resultUpd = await _mediator.Send(
            new Handlers.v1._5.UpdateZaakInformatieObjectCommand
            {
                ZaakInformatieObject = mergedZaakInformatieObject,
                Id = id,
                ZaakUrl = mergedZaakInformatieObjectRequest.Zaak,
                StatusUrl = mergedZaakInformatieObjectRequest.Status,
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

        var zaakInformatieObjectResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakInformatieObjectResponseDto>(resultUpd.Result);

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
    [HttpDelete(Contracts.v1._5.ApiRoutes.ZaakInformatieObjecten.Delete, Name = Contracts.v1._5.Operations.ZaakInformatieObjecten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate, AuthorizationScopes.Zaken.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new Handlers.v1._5.DeleteZaakInformatieObjectCommand { Id = id });

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
