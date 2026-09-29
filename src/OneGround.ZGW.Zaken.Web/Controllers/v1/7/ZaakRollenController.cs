using System;
using System.Collections.Generic;
using System.Linq;
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
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

// Note: Only GetAllAsync/GetAsync/HeadAsync use the new (v1.7) ExpandEngine and the new
// v1._7.Responses.ZaakRol.RolResponseDto (which implements IExpandable). Add/Delete's payload does
// not change for 1.7, so they keep using the v1 (unversioned) Request/Response DTOs and the v1
// MediatR commands they already send -- same reasoning as Controllers/v1/7/ZaakResultatenController.cs.
// Rol has no Update/PartialUpdate action at any version. There is no "fields" mechanism here --
// unlike Zaken's /_zoek, the ZRC 1.7.0 spec only adds "expand" for rollen.
// GetAll/Get/Head instead send the v1._5-namespaced GetAllZaakRolQuery/GetZaakRolQuery (fully
// qualified below, since Handlers.v1 and Handlers.v1._5 both declare same-named types and can't be
// blanket-imported together): only the v1._5 handlers Include ContactpersoonRol and
// Zaak.ZaakStatussen, matching v1.5's own ZaakRollenController -- the v1 (base) handlers predate
// ContactpersoonRol support and leave the field null.
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaakRollenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<RolResponseDto> _expandValidator;
    private readonly ExpandEngine<RolResponseDto> _expandEngine;

    public ZaakRollenController(
        ILogger<ZaakRollenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<RolResponseDto> expandValidator,
        ExpandEngine<RolResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
    }

    /// <summary>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(ApiRoutes.ZaakRollen.GetAll, Name = Operations.ZaakRollen.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<RolResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakRollenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakRollenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

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

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakRollenPageSize));
        var filter = _mapper.Map<GetAllZaakRollenFilter>(queryParameters);

        var result = await _mediator.Send(
            new OneGround.ZGW.Zaken.Web.Handlers.v1._5.GetAllZaakRolQuery { GetAllZaakRolFilter = filter, Pagination = pagination }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var response = _mapper.Map<List<RolResponseDto>>(result.Result.PageResult);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(response, expandPaths);
            }
            catch (ExpandExternalServiceException ex)
            {
                return ExterneServiceFout(ex.ServiceName, ex.ServiceUrl);
            }
            catch (ExpandInternalQueryHandlerException ex)
            {
                return InterneQueryHandlerFout(ex.Resource, ex.StatusCode);
            }
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, response, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "rol" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke ROL bij een ZAAK opvragen.
    /// </summary>
    /// <param name="queryParameters"></param>
    /// <param name="id">Unieke resource identifier (UUID4)</param>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(ApiRoutes.ZaakRollen.Get, Name = Operations.ZaakRollen.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(RolResponseDto))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakRolQueryParameters>))]
    public async Task<IActionResult> GetAsync([FromQuery] GetZaakRolQueryParameters queryParameters, Guid id)
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

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakRolQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<RolResponseDto>(result.Result);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(response, expandPaths);
            }
            catch (ExpandExternalServiceException ex)
            {
                return ExterneServiceFout(ex.ServiceName, ex.ServiceUrl);
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
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "rol" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifieke ROL bij een ZAAK opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakRollen.Get, Name = Contracts.v1._5.Operations.ZaakRollen.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakRolQueryParameters>))]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetZaakRolQueryParameters queryParameters)
    {
        return GetAsync(queryParameters, id);
    }

    /// <summary>
    /// Maak een ROL aan bij een ZAAK.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakRollen.Create, Name = Operations.ZaakRollen.Create)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(Zaken.Contracts.v1.Responses.ZaakRol.ZaakRolResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] Zaken.Contracts.v1.Requests.ZaakRol.ZaakRolRequestDto zaakRolRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakRolRequest);

        ZaakRol zaakrol = _mapper.Map<ZaakRol>(zaakRolRequest);

        var result = await _mediator.Send(new Handlers.v1.CreateZaakRolCommand { ZaakRol = zaakrol, ZaakUrl = zaakRolRequest.Zaak });

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

        var response = _mapper.Map<Zaken.Contracts.v1.Responses.ZaakRol.ZaakRolResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Verwijder een ROL van een ZAAK.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakRollen.Delete, Name = Operations.ZaakRollen.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new Handlers.v1.DeleteZaakRolCommand { Id = id });

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
