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
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._7;
using OneGround.ZGW.Zaken.Web.Models.v1._7;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_7)]
public class ZaakNotitiesController : ZGWControllerBase
{
    private const int MaxPageSize = 500;

    private readonly IPaginationHelper _paginationHelper;
    private readonly IRequestMerger _requestMerger;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakNotitiesController(
        ILogger<ZaakNotitiesController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _requestMerger = requestMerger;
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>() ?? new ApplicationConfiguration();
    }

    /// <summary>
    /// **EXPERIMENTEEL** Alle ZAAKNOTITIEs opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakNotities.GetAll, Name = Operations.ZaakNotities.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakNotitieResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZaakNotitiesQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakNotitiesQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        // Note: pageSize is optional (default from configuration) and is limited to the maximum allowed by the standard
        var pageSize =
            queryParameters.PageSize != null
                ? Math.Min(int.Parse(queryParameters.PageSize), MaxPageSize)
                : _applicationConfiguration.ZaakNotitiesPageSize;

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, pageSize));
        var filter = _mapper.Map<GetAllZaakNotitiesFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakNotitiesQuery { GetAllZaakNotitiesFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var response = _mapper.Map<List<ZaakNotitieResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, response, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaaknotitie" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// **EXPERIMENTEEL** Een specifieke ZAAKNOTITIE opvragen.
    /// </summary>
    /// <param name="uuid">Unieke resource identifier (UUID4)</param>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakNotities.Get, Name = Operations.ZaakNotities.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakNotitieResponseDto))]
    public async Task<IActionResult> GetAsync(Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), uuid);

        var result = await _mediator.Send(new GetZaakNotitieQuery { Id = uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakNotitieResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaaknotitie" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// **EXPERIMENTEEL** Maak een ZAAKNOTITIE aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakNotities.Create, Name = Operations.ZaakNotities.Create)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakNotitieResponseDto))]
    [ServiceFilter(typeof(ValidateBodyParametersFilter<ZaakNotitieRequestDto>))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakNotitieRequestDto zaakNotitieRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakNotitieRequest);

        var zaakNotitie = _mapper.Map<ZaakNotitie>(zaakNotitieRequest);

        var result = await _mediator.Send(new CreateZaakNotitieCommand { ZaakNotitie = zaakNotitie, ZaakUrl = zaakNotitieRequest.GerelateerdAan });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return ForbiddenResult(result.Errors);
        }

        var response = _mapper.Map<ZaakNotitieResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// **EXPERIMENTEEL** Werk een ZAAKNOTITIE in zijn geheel bij.
    /// Een notitie kan alleen gewijzigd worden zolang de status concept is.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakNotities.Update, Name = Operations.ZaakNotities.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakNotitieResponseDto))]
    [ServiceFilter(typeof(ValidateBodyParametersFilter<ZaakNotitieRequestDto>))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakNotitieRequestDto zaakNotitieRequest, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakNotitieRequest, uuid);

        return await UpdateCoreAsync(zaakNotitieRequest, uuid, isPartialUpdate: false);
    }

    /// <summary>
    /// **EXPERIMENTEEL** Werk een ZAAKNOTITIE deels bij.
    /// Een notitie kan alleen gewijzigd worden zolang de status concept is.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakNotities.Update, Name = Operations.ZaakNotities.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakNotitieResponseDto))]
    [ServiceFilter(typeof(ValidateBodyParametersFilter<ZaakNotitieRequestDto>))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakNotitieRequest, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), uuid);

        var resultGet = await _mediator.Send(new GetZaakNotitieQuery { Id = uuid });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var mergedRequest = _requestMerger.MergePartialUpdateToObjectRequest<ZaakNotitieRequestDto, ZaakNotitie>(
            resultGet.Result,
            partialZaakNotitieRequest
        );

        // Note: The merged request is validated like a complete request
        if (!_validatorService.IsValid(mergedRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        return await UpdateCoreAsync(mergedRequest, uuid, isPartialUpdate: true);
    }

    /// <summary>
    /// **EXPERIMENTEEL** Verwijder een ZAAKNOTITIE.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakNotities.Delete, Name = Operations.ZaakNotities.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    public async Task<IActionResult> DeleteAsync(Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), uuid);

        var result = await _mediator.Send(new DeleteZaakNotitieCommand { Id = uuid });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return ForbiddenResult(result.Errors);
        }

        return NoContent();
    }

    private async Task<IActionResult> UpdateCoreAsync(ZaakNotitieRequestDto zaakNotitieRequest, Guid uuid, bool isPartialUpdate)
    {
        var zaakNotitie = _mapper.Map<ZaakNotitie>(zaakNotitieRequest);

        var result = await _mediator.Send(
            new UpdateZaakNotitieCommand
            {
                Id = uuid,
                ZaakNotitie = zaakNotitie,
                ZaakUrl = zaakNotitieRequest.GerelateerdAan,
                IsPartialUpdate = isPartialUpdate,
                KeepNotitieType = string.IsNullOrEmpty(zaakNotitieRequest.NotitieType),
                KeepStatus = string.IsNullOrEmpty(zaakNotitieRequest.Status),
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
            return ForbiddenResult(result.Errors);
        }

        return Ok(_mapper.Map<ZaakNotitieResponseDto>(result.Result));
    }

    // Note: The errors (e.g. a closed zaak) explain the 403, a missing authorization has none
    private IActionResult ForbiddenResult(IEnumerable<ValidationError> errors) =>
        errors != null && errors.Any() ? _errorResponseBuilder.Forbidden(errors) : _errorResponseBuilder.Forbidden();
}
