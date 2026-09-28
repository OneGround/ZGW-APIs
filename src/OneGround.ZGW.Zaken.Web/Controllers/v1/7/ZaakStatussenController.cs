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
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1._5;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

// Note: Only GetAllAsync/GetAsync/HeadAsync use the new (v1.7) ExpandEngine and the new
// v1._7.Responses.StatusResponseDto (which implements IExpandable). AddAsync's payload does not
// change for 1.7, so it keeps using the v1._5 Request/Response DTOs and the v1._5 MediatR command it
// already sends -- same reasoning as Controllers/v1/7/ZakenController.cs. There is no "fields"
// mechanism here -- unlike Zaken's /_zoek, the ZRC 1.7.0 spec only adds "expand" for statussen.
[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaakStatussenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<StatusResponseDto> _expandValidator;
    private readonly ExpandEngine<StatusResponseDto> _expandEngine;

    public ZaakStatussenController(
        ILogger<ZaakStatussenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<StatusResponseDto> expandValidator,
        ExpandEngine<StatusResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;
    }

    /// <summary>
    /// Alle STATUSsen van ZAAKen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(ApiRoutes.ZaakStatussen.GetAll, Name = Operations.ZaakStatussen.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<StatusResponseDto>))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakStatussenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakStatussenQueryParameters queryParameters, int page = 1)
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

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakStatussenPageSize));
        var filter = _mapper.Map<Models.v1._5.GetAllZaakStatussenFilter>(queryParameters);

        var result = await _mediator.Send(
            new Handlers.v1._5.GetAllZaakStatussenQuery { GetAllZaakStatussenFilter = filter, Pagination = pagination }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakStatussenResponse = _mapper.Map<List<StatusResponseDto>>(result.Result.PageResult);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(zaakStatussenResponse, expandPaths);
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

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaakStatussenResponse, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "status" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke STATUS van een ZAAK opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(ApiRoutes.ZaakStatussen.Get, Name = Operations.ZaakStatussen.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(StatusResponseDto))]
    [ETagFilter]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakStatusQueryParameters>))]
    public async Task<IActionResult> GetAsync([FromQuery] GetZaakStatusQueryParameters queryParameters, Guid id)
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

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakStatusQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakStatusResponse = _mapper.Map<StatusResponseDto>(result.Result);

        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(zaakStatusResponse, expandPaths);
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
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "status" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(zaakStatusResponse);
    }

    /// <summary>
    /// De headers voor een specifiek(e) STATUS opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakStatussen.Get, Name = Operations.ZaakStatussen.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetZaakStatusQueryParameters>))]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetZaakStatusQueryParameters queryParameters)
    {
        return GetAsync(queryParameters, id);
    }

    /// <summary>
    /// Maak een STATUS aan voor een ZAAK.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakStatussen.Create, Name = Operations.ZaakStatussen.Create)]
    [Scope(AuthorizationScopes.Zaken.Create, AuthorizationScopes.Zaken.Statuses.Add, AuthorizationScopes.Zaken.Reopen)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(Zaken.Contracts.v1._5.Responses.ZaakStatusCreateResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakStatusRequestDto zaakStatusRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakStatusRequest);

        ZaakStatus zaakstatus = _mapper.Map<ZaakStatus>(zaakStatusRequest);

        var result = await _mediator.Send(new Handlers.v1._5.CreateZaakStatusCommand { ZaakStatus = zaakstatus, ZaakUrl = zaakStatusRequest.Zaak });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakStatusResponse = _mapper.Map<Zaken.Contracts.v1._5.Responses.ZaakStatusCreateResponseDto>(result.Result);

        return Created(zaakStatusResponse.Url, zaakStatusResponse);
    }
}
