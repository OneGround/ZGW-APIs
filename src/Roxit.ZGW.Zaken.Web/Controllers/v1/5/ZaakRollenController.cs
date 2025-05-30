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
using Roxit.ZGW.Common.Web.Filters;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;
using Roxit.ZGW.Zaken.DataModel.ZaakRol;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Configuration;
using Roxit.ZGW.Zaken.Web.Contracts.v1._5;
using Roxit.ZGW.Zaken.Web.Handlers.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Zaken.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_5)]
public class ZaakRollenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakRollenController(
        ILogger<ZaakRollenController> logger,
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
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakRollen.GetAll, Name = Operations.ZaakRollen.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakRolResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] Zaken.Contracts.v1.Queries.GetAllZaakRollenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakRollenPageSize));
        var filter = _mapper.Map<Models.v1.GetAllZaakRollenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakRolQuery { GetAllZaakRolFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakRolResponse = _mapper.Map<List<ZaakRolResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaakRolResponse, result.Result.Count);

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
    /// <param name="id">Unieke resource identifier (UUID4)</param>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakRollen.Get, Name = Operations.ZaakRollen.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakRolResponseDto))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakRolQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakRolResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "rol" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ROL opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakRollen.Get, Name = Operations.ZaakRollen.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
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
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakRolResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakRolRequestDto zaakRolRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakRolRequest);

        ZaakRol zaakrol = _mapper.Map<ZaakRol>(zaakRolRequest);

        var result = await _mediator.Send(new CreateZaakRolCommand { ZaakRol = zaakrol, ZaakUrl = zaakRolRequest.Zaak });

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

        var zaakRolResponse = _mapper.Map<ZaakRolResponseDto>(result.Result);

        return Created(zaakRolResponse.Url, zaakRolResponse);
    }

    // HTTP DELETE http://zaken.user.local:5005/api/v1/rollen/59bad509-840b-4cd0-82dc-cbda74a75c2b

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

        var result = await _mediator.Send(new DeleteZaakRolCommand { Id = id });

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
