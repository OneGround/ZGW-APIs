using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Configuration;
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
public class ZaakStatussenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakStatussenController(
        ILogger<ZaakStatussenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
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
    [HttpGet(ApiRoutes.ZaakStatussen.GetAll, Name = Operations.ZaakStatussen.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakStatusResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakStatussenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakStatussenPageSize));
        var filter = _mapper.Map<GetAllZaakStatussenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakStatussenQuery { GetAllZaakStatussenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakStatussenResponse = _mapper.Map<List<ZaakStatusResponseDto>>(result.Result.PageResult);

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
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakStatussen.Get, Name = Operations.ZaakStatussen.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakStatusResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakStatusQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakStatusResponse = _mapper.Map<ZaakStatusResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "status" },
            }
        );

        return Ok(zaakStatusResponse);
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
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakStatusResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakStatusRequestDto zaakStatusRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakStatusRequest);

        ZaakStatus zaakstatus = _mapper.Map<ZaakStatus>(zaakStatusRequest);

        var result = await _mediator.Send(new CreateZaakStatusCommand { ZaakStatus = zaakstatus, ZaakUrl = zaakStatusRequest.Zaak });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakStatusResponse = _mapper.Map<ZaakStatusResponseDto>(result.Result);

        return Created(zaakStatusResponse.Url, zaakStatusResponse);
    }
}
