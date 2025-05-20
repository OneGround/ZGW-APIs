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
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using OneGround.ZGW.Zaken.Web.Models.v1;
using OneGround.ZGW.Zaken.Web.Validators.v1;
using Swashbuckle.AspNetCore.Annotations;

//
// Bron ZRC API:       https://zaken-api.vng.cloud/api/v1/schema/
// Bron ZGW standaard: https://vng-realisatie.github.io/gemma-zaken/standaard/

// Bron Alg tools VNG: https://github.com/VNG-Realisatie/vng-api-common

namespace OneGround.ZGW.Zaken.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
public class ZakenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZakenController(
        ILogger<ZakenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Alle ZAAKen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <remarks>
    /// Er worden enkel zaken getoond van de zaaktypes waar u toe geautoriseerd bent.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Zaken.GetAll, Name = Operations.Zaken.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakResponseDto>))]
    [RequiresAcceptCrs]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZakenQueryParameters queryParameters, int page = 1, string ordering = null)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}, {Ordering}", nameof(GetAllAsync), queryParameters, page, ordering);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));
        var filter = _mapper.Map<GetAllZakenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllZakenQuery
            {
                GetAllZakenFilter = filter,
                Pagination = pagination,
                Ordering = ordering,
                SRID = GetSridFromAcceptCrsHeader(),
            }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zakenResponse = _mapper.Map<List<ZaakResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zakenResponse, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke ZAAK opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Zaken.Get, Name = Operations.Zaken.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [RequiresAcceptCrs]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakResponse = _mapper.Map<ZaakResponseDto>(result.Result);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                BaseEntity = result.Result,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" },
            }
        );

        return Ok(zaakResponse);
    }

    /// <summary>
    /// Voer een (geo)-zoekopdracht uit op ZAAKen.
    /// </summary>
    /// <remarks>
    /// Zoeken/filteren gaat normaal via de list operatie, deze is echter niet geschikt voor geo-zoekopdrachten.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Zaken.Search, Name = Operations.Zaken.Search)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakResponseDto>))]
    [RequiresAcceptCrs]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> SearchAsync([FromBody] ZaakSearchRequestDto zaakSearchRequest, int page = 1, string ordering = null)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Page}, {Ordering}", nameof(SearchAsync), zaakSearchRequest, page, ordering);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));
        var filter = _mapper.Map<GetAllZakenFilter>(zaakSearchRequest);

        var result = await _mediator.Send(
            new GetAllZakenQuery
            {
                GetAllZakenFilter = filter,
                WithinZaakGeometry = zaakSearchRequest.ZaakGeometry.Within,
                Pagination = pagination,
                Ordering = ordering,
                SRID = GetSridFromAcceptCrsHeader(),
            }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zakenResponse = _mapper.Map<List<ZaakResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(pagination, zakenResponse, result.Result.Count);

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" },
            }
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Maak een ZAAK aan.
    /// Indien geen identificatie gegeven is, dan wordt deze automatisch gegenereerd. De identificatie moet uniek zijn binnen de bronorganisatie.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Zaken.Create, Name = Operations.Zaken.Create)]
    [Scope(AuthorizationScopes.Zaken.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> AddAsync([FromBody] ZaakRequestDto zaakRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Rsin}", nameof(AddAsync), zaakRequest, zaakRequest.Bronorganisatie);

        var zaak = _mapper.Map<Zaak>(zaakRequest);

        int? srid = null;
        if (zaak.Zaakgeometrie != null)
        {
            srid = TryGetSridFromContentCrsHeader();
            if (!srid.HasValue)
            {
                return _errorResponseBuilder.NotAcceptable(
                    "The value specified in Accept-Crs header differs from the value specified in Content-Crs header. (Not supported yet)"
                );
            }
            zaak.Zaakgeometrie.SRID = srid.Value;
        }

        var result = await _mediator.Send(
            new CreateZaakCommand
            {
                Zaak = zaak,
                HoofdzaakUrl = zaakRequest.Hoofdzaak,
                SRID = srid,
            }
        );

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var zaakResponse = _mapper.Map<ZaakResponseDto>(result.Result);

        return Created(zaakResponse.Url, zaakResponse);
    }

    /// <summary>
    /// Werk een ZAAK in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.Zaken.Update, Name = Operations.Zaken.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakRequestDto zaakRequest, Guid id)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {@FromBody}, {Uuid}, {Rsin}",
            nameof(UpdateAsync),
            zaakRequest,
            id,
            zaakRequest.Bronorganisatie
        );

        var resultGet = await _mediator.Send(new GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaak = _mapper.Map<Zaak>(zaakRequest);

        int? srid = null;
        if (zaak.Zaakgeometrie != null)
        {
            srid = TryGetSridFromContentCrsHeader();
            if (!srid.HasValue)
            {
                return _errorResponseBuilder.NotAcceptable(
                    "The value specified in Accept-Crs header differs from the value specified in Content-Crs header. (Not supported yet)"
                );
            }
            zaak.Zaakgeometrie.SRID = srid.Value;
        }

        var result = await _mediator.Send(
            new UpdateZaakCommand
            {
                Zaak = zaak,
                OriginalZaak = resultGet.Result,
                Id = id,
                HoofdzaakUrl = zaakRequest.Hoofdzaak,
                SRID = srid,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakResponse = _mapper.Map<ZaakResponseDto>(result.Result);

        return Ok(zaakResponse);
    }

    /// <summary>
    /// Werk een ZAAK deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.Zaken.Update, Name = Operations.Zaken.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var mergedZaakRequest = _requestMerger.MergePartialUpdateToObjectRequest<ZaakRequestDto, Zaak>(resultGet.Result, partialZaakRequest);

        int? srid = null;
        if (mergedZaakRequest.Zaakgeometrie != null)
        {
            srid = TryGetSridFromContentCrsHeader();
            if (!srid.HasValue)
            {
                return _errorResponseBuilder.NotAcceptable(
                    "The value specified in Accept-Crs header differs from the value specified in Content-Crs header. (Not supported yet)"
                );
            }
            mergedZaakRequest.Zaakgeometrie.SRID = srid.Value;
        }

        var passesMergedZaakValidation = _validatorService.IsValid(mergedZaakRequest, out var validationResult);
        var passesPatchValidation = _validatorService.IsValid<PatchZaakValidationDto>(partialZaakRequest, out var preMergevalidationResult);

        if (!passesMergedZaakValidation || !passesPatchValidation)
        {
            return _errorResponseBuilder.BadRequest(validationResult, preMergevalidationResult);
        }

        var mergedZaak = _mapper.Map<Zaak>(mergedZaakRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakCommand
            {
                Zaak = mergedZaak,
                OriginalZaak = resultGet.Result,
                Id = id,
                HoofdzaakUrl = mergedZaakRequest.Hoofdzaak,
                IsPartialUpdate = true,
                SRID = mergedZaakRequest.Zaakgeometrie != null ? srid.Value : null,
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

        var zaakResponse = _mapper.Map<ZaakResponseDto>(resultUpd.Result);

        return Ok(zaakResponse);
    }

    /// <summary>
    /// Verwijder een ZAAK.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.Zaken.Delete, Name = Operations.Zaken.Delete)]
    [Scope(AuthorizationScopes.Zaken.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Alle audit trail regels behorend bij de ZAAK.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakAudittrail.GetAll, Name = Operations.ZaakAudittrail.List)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<AuditTrailRegelDto>))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> GetAllZaakAuditTrailRegelsAsync(Guid zaak_uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {@ZaakUuid}", nameof(GetAllZaakAuditTrailRegelsAsync), zaak_uuid);

        var result = await _mediator.Send(new GetAllZaakAuditTrailRegels { ZaakId = zaak_uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakAuditTrailRegelsResponse = _mapper.Map<List<AuditTrailRegelDto>>(result.Result);

        return Ok(zaakAuditTrailRegelsResponse);
    }

    //
    // HTTP GET http://zaken.user.local:5005/api/v1/zaken/59bad509-840b-4cd0-82dc-cbda74a75c2b/audittrail/782b4144-0185-4180-8b59-2ce322dad69d

    /// <summary>
    /// Een specifieke audit trail regel opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakAudittrail.Get, Name = Operations.ZaakAudittrail.Read)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AuditTrailRegelDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> GetZaakAuditTrailRegelAsync(Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {Uuid}", nameof(GetZaakAuditTrailRegelAsync), zaak_uuid, uuid);

        var result = await _mediator.Send(new GetZaakAuditTrailRegel { ZaakId = zaak_uuid, AuditTrailRegelId = uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakAuditTrailRegelResponse = _mapper.Map<AuditTrailRegelDto>(result.Result);

        return Ok(zaakAuditTrailRegelResponse);
    }

    /// <summary>
    /// Alle ZAAKBESLUITen opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakBesluiten.GetAll, Name = Operations.ZaakBesluiten.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<ZaakBesluitResponseDto>))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> GetAllZaakBesluitenAsync(Guid zaak_uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}", nameof(GetAllZaakBesluitenAsync), zaak_uuid);

        var result = await _mediator.Send(new GetAllZaakBesluiten { Zaak = zaak_uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<IEnumerable<ZaakBesluitResponseDto>>(result.Result).ToList();

        // Note: Should this action to be recorded in audittrail?
        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = response.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakbesluit" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een ZAAKBESLUIT aan.
    /// </summary>
    /// <remarks>
    /// De Besluiten API gebruikt dit endpoint om relaties te synchroniseren, daarom is dit endpoint in de Zaken API geimplementeerd.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakBesluiten.Create, Name = Operations.ZaakBesluiten.Create)]
    [Scope(AuthorizationScopes.Zaken.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakBesluitResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> AddZaakBesluitenAsync(Guid zaak_uuid, [FromBody] ZaakBesluitRequestDto zaakBesluitRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {@FromBody}", nameof(AddZaakBesluitenAsync), zaak_uuid, zaakBesluitRequest);

        var zaakBesluit = _mapper.Map<ZaakBesluit>(zaakBesluitRequest);

        var result = await _mediator.Send(new CreateZaakBesluitCommand { ZaakId = zaak_uuid, Besluit = zaakBesluit });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakBesluitResponse = _mapper.Map<ZaakBesluitResponseDto>(result.Result);

        return Created(zaakBesluitResponse.Url, zaakBesluitResponse);
    }

    /// <summary>
    /// Een specifiek ZAAKBESLUIT opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakBesluiten.Get, Name = Operations.ZaakBesluiten.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakBesluitResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> GetZaakBesluitAsync(Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {Uuid}", nameof(GetZaakBesluitAsync), zaak_uuid, uuid);

        var result = await _mediator.Send(new GetZaakBesluiten { Zaak = zaak_uuid, Besluit = uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakBesluitResponseDto>(result.Result);

        // Note: Should this action to be recorded in audittrail?
        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakbesluit" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Verwijder een ZAAKBESLUIT.
    /// </summary>
    /// <remarks>
    /// LET OP: Dit endpoint hoor je als consumer niet zelf aan te spreken.
    /// De Besluiten API gebruikt dit endpoint om relaties te synchroniseren, daarom is dit endpoint in de Zaken API geimplementeerd.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakBesluiten.Delete, Name = Operations.ZaakBesluiten.Delete)]
    [Scope(AuthorizationScopes.Zaken.Update)]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteZaakBesluitAsync(Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {Uuid}", nameof(DeleteZaakBesluitAsync), zaak_uuid, uuid);

        var result = await _mediator.Send(new DeleteZaakBesluitCommand { ZaakId = zaak_uuid, Id = uuid });

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
    /// Alle ZAAKEIGENSCHAPpen opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakEigenschappen.GetAll, Name = Operations.ZaakEigenschappen.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<ZaakEigenschapResponseDto>))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> GetAllZaakEigenschappenAsync(Guid zaak_uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}", nameof(GetAllZaakEigenschappenAsync), zaak_uuid);

        var result = await _mediator.Send(new GetAllZaakEigenschappenQuery { Zaak = zaak_uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<IEnumerable<ZaakEigenschapResponseDto>>(result.Result).ToList();

        // Note: Should this action to be recorded in audittrail?
        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                TotalCount = response.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakeigenschap" },
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// Maak een ZAAKEIGENSCHAP aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakEigenschappen.Create, Name = Operations.ZaakEigenschappen.Create)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakEigenschapResponseDto))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> AddZaakEigenschapAsync(Guid zaak_uuid, [FromBody] ZaakEigenschapRequestDto zaakEigenschapRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {@FromBody}", nameof(AddZaakEigenschapAsync), zaak_uuid, zaakEigenschapRequest);

        var zaakEigenschap = _mapper.Map<ZaakEigenschap>(zaakEigenschapRequest);

        var result = await _mediator.Send(new CreateZaakEigenschapCommand { ZaakId = zaak_uuid, ZaakEigenschap = zaakEigenschap });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaakEigenschapResponse = _mapper.Map<ZaakEigenschapResponseDto>(result.Result);

        return Created(zaakEigenschapResponse.Url, zaakEigenschapResponse);
    }

    /// <summary>
    /// Een specifieke ZAAKEIGENSCHAP opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakEigenschappen.Get, Name = Operations.ZaakEigenschappen.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_2)]
    public async Task<IActionResult> GetZaakEigenschapAsync(Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {Uuid}", nameof(GetZaakEigenschapAsync), zaak_uuid, uuid);

        var result = await _mediator.Send(new GetZaakEigenschapQuery { Zaak = zaak_uuid, Eigenschap = uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<ZaakEigenschapResponseDto>(result.Result);

        // Note: Should this action to be recorded in audittrail?
        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakeigenschap" },
            }
        );

        return Ok(response);
    }
}
