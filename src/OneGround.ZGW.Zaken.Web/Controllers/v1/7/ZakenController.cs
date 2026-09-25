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
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._7;

// Note: Only GetAllAsync, SearchAsync, GetAsync and HeadAsync use the new (v1.7) ExpandEngine and
// the new v1._7.Responses.ZaakResponseDto (which implements IExpandable). The other actions'
// payload does not change for 1.7, so they keep using the v1._5 Request/Response DTOs and the
// v1._5 MediatR queries/commands they already send -- there is no v1.7-specific domain logic, so
// duplicating those into their own Handlers/v1/7 would only add upkeep for code that never diverges.
[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_7)]
[Consumes("application/json")]
[Produces("application/json")]
public class ZakenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IRequestMerger _requestMerger;
    private readonly ExpandValidator<ZaakResponseDto> _expandValidator;
    private readonly FieldsValidator<ZaakResponseDto> _fieldsValidator;
    private readonly ExpandEngine<ZaakResponseDto> _expandEngine;

    public ZakenController(
        ILogger<ZakenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<ZaakResponseDto> expandValidator,
        FieldsValidator<ZaakResponseDto> fieldsValidator,
        ExpandEngine<ZaakResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _requestMerger = requestMerger;
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expandValidator = expandValidator;
        _fieldsValidator = fieldsValidator;
        _expandEngine = expandEngine;
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
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.Zaken.GetAll, Name = Contracts.v1._5.Operations.Zaken.List)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakResponseDto>))]
    [RequiresAcceptCrs]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<Zaken.Contracts.v1._5.Queries.GetAllZakenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Zaken.Contracts.v1._5.Queries.GetAllZakenQueryParameters queryParameters,
        int page = 1,
        string ordering = null
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}, {Ordering}", nameof(GetAllAsync), queryParameters, page, ordering);

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

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));
        var filter = _mapper.Map<Models.v1._5.GetAllZakenFilter>(queryParameters);

        var result = await _mediator.Send(
            new Handlers.v1._5.GetAllZakenQuery
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

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(zakenResponse, expandPaths);
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
    /// <response code="502">Bad Gateway</response>
    [HttpPost(Contracts.v1._5.ApiRoutes.Zaken.Search, Name = Contracts.v1._5.Operations.Zaken.Search)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakResponseDto>))]
    [RequiresAcceptCrs]
    [ServiceFilter(typeof(ValidateBodyParametersFilter<ZaakSearchRequestDto>))]
    public async Task<IActionResult> SearchAsync([FromBody] ZaakSearchRequestDto zaakSearchRequest, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Page}", nameof(SearchAsync), zaakSearchRequest, page);

        FieldSelection fieldSelection = null;
        List<string> expandPaths;

        if (zaakSearchRequest.Fields is not null)
        {
            // Use the new expand- and field-selection (> v1.5)
            var (selection, impliedExpands, fieldsError) = FieldsParser.ParseAndValidate(zaakSearchRequest.Fields);
            if (fieldsError is not null)
            {
                return _errorResponseBuilder.BadRequest(
                    new[] { new ValidationError("fields", ErrorCode.Invalid, fieldsError) },
                    title: "Ongeldige fields parameter."
                );
            }

            var invalidFields = _fieldsValidator.Validate(selection);
            if (invalidFields.Count > 0)
            {
                var fieldErrors = invalidFields
                    .Select(path => new ValidationError("fields", ErrorCode.Invalid, $"Ongeldige veldnaam in 'fields': {path}."))
                    .ToArray();

                return _errorResponseBuilder.BadRequest(fieldErrors, title: "Ongeldige fields parameter");
            }

            if (!IsExpandEnabled(_applicationConfiguration.ExpandSettings.Search, impliedExpands))
            {
                return _errorResponseBuilder.BadRequest(
                    new[] { new ValidationError("fields", ErrorCode.DisabledExpand, "Expand is uitgeschakeld op deze operatie.") },
                    title: "Invalid input"
                );
            }

            fieldSelection = selection;
            expandPaths = impliedExpands;
        }
        else
        {
            // Use the legacy expand-only path (= v1.5)
            var expandValidationResult = ValidateExpand(
                _expandValidator,
                zaakSearchRequest.Expand,
                _applicationConfiguration.ExpandSettings.Search,
                out var paths
            );
            if (expandValidationResult is not null)
            {
                return expandValidationResult;
            }
            expandPaths = paths;
        }

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));
        var filter = _mapper.Map<Models.v1._5.GetAllZakenFilter>(zaakSearchRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.GetAllZakenQuery
            {
                GetAllZakenFilter = filter,
                WithinZaakGeometry = zaakSearchRequest.ZaakGeometry?.Within,
                Pagination = pagination,
                Ordering = zaakSearchRequest.Ordering,
                SRID = GetSridFromAcceptCrsHeader(),
            }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zakenResponse = _mapper.Map<List<ZaakResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(zakenResponse, expandPaths);
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

        List<object> responseResults;
        if (zaakSearchRequest.Fields is not null)
        {
            responseResults = FieldProjector.ProjectList(zakenResponse, fieldSelection).Cast<object>().ToList();
        }
        else
        {
            responseResults = zakenResponse.Cast<object>().ToList();
        }

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(pagination, responseResults, result.Result.Count);

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
    [HttpPost(Contracts.v1._5.ApiRoutes.Zaken.Create, Name = Contracts.v1._5.Operations.Zaken.Create)]
    [Scope(AuthorizationScopes.Zaken.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
    public async Task<IActionResult> AddAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakRequestDto zaakRequest)
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
            new Handlers.v1._5.CreateZaakCommand
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
    /// Een specifieke ZAAK opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="412">Precondition Failed</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.Zaken.Get, Name = Contracts.v1._5.Operations.Zaken.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [RequiresAcceptCrs]
    [ETagFilter]
    public async Task<IActionResult> GetAsync([FromQuery] Zaken.Contracts.v1._5.Queries.GetZaakQueryParameters queryParameters, Guid id)
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

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var zaak = _mapper.Map<ZaakResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(zaak, expandPaths);
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
                RetrieveCatagory = RetrieveCatagory.Minimal,
                BaseEntity = result.Result,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" },
                LegacyAuditTrail = result.Result.LegacyAuditTrail,
            }
        );

        return Ok(zaak);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ZAAK opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="502">Bad Gateway</response>
    [HttpHead(Contracts.v1._5.ApiRoutes.Zaken.Get, Name = Contracts.v1._5.Operations.Zaken.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] Zaken.Contracts.v1._5.Queries.GetZaakQueryParameters queryParameters)
    {
        return GetAsync(queryParameters, id);
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
    [HttpPut(Contracts.v1._5.ApiRoutes.Zaken.Update, Name = Contracts.v1._5.Operations.Zaken.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
    public async Task<IActionResult> UpdateAsync([FromBody] Zaken.Contracts.v1._5.Requests.ZaakRequestDto zaakRequest, Guid id)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {@FromBody}, {Uuid}, {Rsin}",
            nameof(UpdateAsync),
            zaakRequest,
            id,
            zaakRequest.Bronorganisatie
        );

        var resultGet = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

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
            new Handlers.v1._5.UpdateZaakCommand
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
    [HttpPatch(Contracts.v1._5.ApiRoutes.Zaken.Update, Name = Contracts.v1._5.Operations.Zaken.PartialUpdate)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = id, SRID = GetSridFromAcceptCrsHeader() });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var mergedZaakRequest = _requestMerger.MergePartialUpdateToObjectRequest<Zaken.Contracts.v1._5.Requests.ZaakRequestDto, Zaak>(
            resultGet.Result,
            partialZaakRequest
        );

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
        var passesPatchValidation = _validatorService.IsValid<Validators.v1._5.PatchZaakValidationDto>(
            partialZaakRequest,
            out var preMergevalidationResult
        );

        if (!passesMergedZaakValidation || !passesPatchValidation)
        {
            return _errorResponseBuilder.BadRequest(validationResult, preMergevalidationResult);
        }

        var mergedZaak = _mapper.Map<Zaak>(mergedZaakRequest);

        var resultUpd = await _mediator.Send(
            new Handlers.v1._5.UpdateZaakCommand
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
    /// Een specifieke ZAAKEIGENSCHAP opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.ZaakEigenschappen.Get, Name = Contracts.v1._5.Operations.ZaakEigenschappen.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    public async Task<IActionResult> GetZaakEigenschapAsync(Guid zaak_uuid, Guid uuid)
    {
        _logger.LogDebug("{ControllerMethod} called with {ZaakUuid}, {Uuid}", nameof(GetZaakEigenschapAsync), zaak_uuid, uuid);

        var result = await _mediator.Send(new Handlers.v1.GetZaakEigenschapQuery { Zaak = zaak_uuid, Eigenschap = uuid });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var response = _mapper.Map<Zaken.Contracts.v1.Responses.ZaakEigenschapResponseDto>(result.Result);

        // Note: Should this action to be recorded in audittrail?
        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.All,
                BaseEntity = result.Result.Zaak,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakeigenschap" },
                LegacyAuditTrail = result.Result.Zaak.LegacyAuditTrail,
            }
        );

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ZAAKEIGENSCHAP opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(Contracts.v1._5.ApiRoutes.ZaakEigenschappen.Get, Name = Contracts.v1._5.Operations.ZaakEigenschappen.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadZaakEigenschapAsync(Guid zaak_uuid, Guid uuid)
    {
        return GetZaakEigenschapAsync(zaak_uuid, uuid);
    }
}
