using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
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

// Note: Only GetAllAsync/GetAsync use the new (v1.7) ExpandEngine and the new
// v1._7.Responses.ZaakContactmomentResponseDto (which implements IExpandable). Add/Delete's payload
// does not change for 1.7, so they keep using the v1._5 Request/Response DTOs and the v1._5 MediatR
// commands they already send -- same reasoning as Controllers/v1/7/ZaakObjectenController.cs.
// ZaakContactmoment has no Update/PartialUpdate/Head action at any version, and GetAll has never been
// paginated here (it returns a plain list, not a PagedResponse) -- v1.7 keeps that shape unchanged, it
// only adds "expand". There is no "fields" mechanism here -- unlike Zaken's /_zoek, the ZRC 1.7.0 spec
// only adds "expand" for zaakcontactmomenten.
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaakContactmomentenController : ZGWControllerBase
{
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<ZaakContactmomentResponseDto> _expandValidator;
    private readonly ExpandEngine<ZaakContactmomentResponseDto> _expandEngine;

    public ZaakContactmomentenController(
        ILogger<ZaakContactmomentenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<ZaakContactmomentResponseDto> expandValidator,
        ExpandEngine<ZaakContactmomentResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
    }

    /// <summary>
    /// Alle ZAAK-CONTACTMOMENTen opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakContactmomenten.GetAll, Name = Contracts.v1._5.Operations.ZaakContactmomenten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IList<ZaakContactmomentResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakContactmomentenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakContactmomentenQueryParameters queryParameters)
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

        var filter = _mapper.Map<Models.v1._5.GetAllZaakContactmomentenFilter>(queryParameters);

        var result = await _mediator.Send(new Handlers.v1._5.GetAllZaakContactmomentenQuery { GetAllZaakContactmomentenFilter = filter });

        var response = _mapper.Map<List<ZaakContactmomentResponseDto>>(result.Result);

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
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakcontactmoment" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Een specifieke ZAAK-CONTACTMOMENT opvragen.
    /// </summary>
    /// <param name="queryParameters"></param>
    /// <param name="id">Unieke resource identifier (UUID4)</param>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakContactmomenten.Get, Name = Contracts.v1._5.Operations.ZaakContactmomenten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakContactmomentResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakContactmomentQueryParameters>))]
    public async Task<IActionResult> GetAsync([FromQuery] GetZaakContactmomentQueryParameters queryParameters, Guid id)
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

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakContactmomentQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakContactmomentResponseDto>(result.Result);

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
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakcontactmoment" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een ZAAK-CONTACTMOMENT aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(Contracts.v1._5.ApiRoutes.ZaakContactmomenten.Create, Name = Contracts.v1._5.Operations.ZaakContactmomenten.Create)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakContactmomentResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakContactmomentRequestDto zaakContactmomentRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakContactmomentRequest);

        ZaakContactmoment zaakcontactmoment = _mapper.Map<ZaakContactmoment>(zaakContactmomentRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.CreateZaakContactmomentCommand { ZaakContactmoment = zaakcontactmoment, ZaakUrl = zaakContactmomentRequest.Zaak }
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

        var zaakContactmomentResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakContactmomentResponseDto>(result.Result);

        return Created(zaakContactmomentResponse.Url, zaakContactmomentResponse);
    }

    /// <summary>
    /// Verwijder een ZAAK-CONTACTMOMENT.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(Contracts.v1._5.ApiRoutes.ZaakContactmomenten.Delete, Name = Contracts.v1._5.Operations.ZaakContactmomenten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new Handlers.v1._5.DeleteZaakContactmomentCommand { Id = id });

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
