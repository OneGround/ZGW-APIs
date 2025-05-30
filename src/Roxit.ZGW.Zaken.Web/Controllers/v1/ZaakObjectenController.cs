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
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Configuration;
using Roxit.ZGW.Zaken.Web.Contracts.v1;
using Roxit.ZGW.Zaken.Web.Handlers.v1;
using Roxit.ZGW.Zaken.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Zaken.Web.Controllers.v1;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_0)]
[Consumes("application/json")]
[Produces("application/json")]
public class ZaakObjectenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakObjectenController(
        ILogger<ZaakObjectenController> logger,
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
    /// Alle ZAAKOBJECTen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjecten.GetAll, Name = Operations.ZaakObjecten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakObjectResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakObjectenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakObjectenPageSize));
        var filter = _mapper.Map<GetAllZaakObjectenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakObjectenQuery { GetAllZaakObjectenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakObjectResponse = _mapper.Map<List<ZaakObjectResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaakObjectResponse, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek ZAAKOBJECT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjecten.Get, Name = Operations.ZaakObjecten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakObjectQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakObjectResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een ZAAKOBJECT aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakObjecten.Create, Name = Operations.ZaakObjecten.Create)]
    [Scope(AuthorizationScopes.Zaken.Create, AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakObjectResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakObjectRequestDto zaakObjectRequest) // Note: zaakObjectRequest can be of types: ZaakObjectRequestDto, RelatieZaakObjectRequestDto or InvalidZaakObjectRequestDto (due ZaakObjectRequestDtoJsonConverter)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakObjectRequest);

        zaakObjectRequest.ObjectTypeOverigeDefinitie = null; // Note: To be sure not added with the 1.0 version!

        ZaakObject zaakObject = _mapper.Map<ZaakObject>(zaakObjectRequest);

        var result = await _mediator.Send(new CreateZaakObjectCommand { ZaakObject = zaakObject, ZaakUrl = zaakObjectRequest.Zaak });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakObjectResponseDto>(result.Result);

        return Created(response.Url, response);
    }
}
