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
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Besluiten.Web.Configuration;
using OneGround.ZGW.Besluiten.Web.Contracts.v1;
using OneGround.ZGW.Besluiten.Web.Expands;
using OneGround.ZGW.Besluiten.Web.Handlers.v1;
using OneGround.ZGW.Besluiten.Web.Models.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Besluiten.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class BesluitenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IObjectExpander<BesluitResponseDto> _expander;

    public BesluitenController(
        ILogger<BesluitenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder,
        IExpanderFactory expanderFactory
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        _expander = expanderFactory.Create<BesluitResponseDto>(ExpanderNames.BesluitExpander);
    }

    /// <summary>
    /// Alle BESLUITen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Besluiten.GetAll, Name = Operations.Besluiten.List)]
    [Scope(AuthorizationScopes.Besluiten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<BesluitResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllBesluitenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.BesluitenPageSize));
        var filter = _mapper.Map<GetAllBesluitenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllBesluitenQuery { GetAllBesluitenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var besluitenResponse = _mapper.Map<List<BesluitResponseDto>>(result.Result.PageResult);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var besluitenWithOptionalExpand = besluitenResponse.Select(z => _expander.ResolveAsync(expandLookup, z).Result);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            besluitenWithOptionalExpand,
            result.Result.Count
        );

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifiek BESLUIT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Besluiten.Get, Name = Operations.Besluiten.Read)]
    [Scope(AuthorizationScopes.Besluiten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitResponseDto))]
    public async Task<IActionResult> GetAsync([FromQuery] GetBesluitenQueryParameters queryParameters, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetBesluitQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<BesluitResponseDto>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var besluitWithOptionalExpand = await _expander.ResolveAsync(expandLookup, response);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                BaseEntity = result.Result,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" },
            }
        );

        return Ok(besluitWithOptionalExpand);
    }

    /// <summary>
    /// Maak een BESLUIT aan.
    /// Indien geen identificatie gegeven is, dan wordt deze automatisch gegenereerd.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Besluiten.Create, Name = Operations.Besluiten.Create)]
    [Scope(AuthorizationScopes.Besluiten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(BesluitResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] BesluitRequestDto besluitRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), besluitRequest);

        Besluit besluit = _mapper.Map<Besluit>(besluitRequest);

        var result = await _mediator.Send(new CreateBesluitCommand { Besluit = besluit });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<BesluitResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Werk een BESLUIT in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.Besluiten.Update, Name = Operations.Besluiten.Update)]
    [Scope(AuthorizationScopes.Besluiten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] BesluitRequestDto besluitRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), besluitRequest, id);

        Besluit besluit = _mapper.Map<Besluit>(besluitRequest);

        var result = await _mediator.Send(new UpdateBesluitCommand { Besluit = besluit, Id = id });

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

        var response = _mapper.Map<BesluitResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Werk een BESLUIT deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.Besluiten.Update, Name = Operations.Besluiten.PartialUpdate)]
    [Scope(AuthorizationScopes.Besluiten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialBesluitRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetBesluitQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        BesluitRequestDto mergedBesluitRequest = _requestMerger.MergePartialUpdateToObjectRequest<BesluitRequestDto, Besluit>(
            resultGet.Result,
            partialBesluitRequest
        );

        if (!_validatorService.IsValid(mergedBesluitRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        Besluit mergedBesluit = _mapper.Map<Besluit>(mergedBesluitRequest);

        var resultUpd = await _mediator.Send(
            new UpdateBesluitCommand
            {
                Id = id,
                Besluit = mergedBesluit,
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

        var response = _mapper.Map<BesluitResponseDto>(resultUpd.Result);

        return Ok(response);
    }

    /// <summary>
    /// Verwijder een BESLUIT samen met alle gerelateerde resources binnen deze API.
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
    [HttpDelete(ApiRoutes.Besluiten.Delete, Name = Operations.Besluiten.Delete)]
    [Scope(AuthorizationScopes.Besluiten.Delete)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteBesluitCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        return NoContent();
    }

    /// <summary>
    /// Alle audit trail regels behorend bij het BESLUIT.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.BesluitAudittrail.GetAll, Name = Operations.BesluitAudittrail.List)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<AuditTrailRegelDto>))]
    public async Task<IActionResult> GetAllBesluitAuditTrailRegelsAsync(Guid besluit_uuid)
    {
        var result = await _mediator.Send(new GetAllBesluitAuditTrailRegels { BesluitId = besluit_uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<List<AuditTrailRegelDto>>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Een specifieke audit trail regel opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.BesluitAudittrail.Get, Name = Operations.BesluitAudittrail.Read)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AuditTrailRegelDto))]
    public async Task<IActionResult> GetBesluitAuditTrailRegelAsync(Guid besluit_uuid, Guid uuid)
    {
        var result = await _mediator.Send(new GetBesluitAuditTrailRegel { BesluitId = besluit_uuid, AuditTrailRegelId = uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<AuditTrailRegelDto>(result.Result);

        return Ok(response);
    }
}
