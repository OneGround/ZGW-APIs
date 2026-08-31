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
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Contracts.v1._5;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using OneGround.ZGW.Zaken.Web.Validators.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Zaken.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_5)]
[Consumes("application/json")]
[Produces("application/json")]
public class ZakenController : ZGWControllerBase
{
    // Feature-vlag (bewust hardcoded, geen configuratie): 'resource'/'resource_id' op een
    // _snapshots-item zijn geen onderdeel van de VNG-standaard "Synchronisatie van collecties".
    // Op false laat ZaakSnapshotDto's NullValueHandling.Ignore deze velden geheel weg, zodat de
    // response strikt aan de standaard voldoet.
    private static readonly bool IncludeNonStandardSnapshotFields = false;

    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IObjectExpander<ZaakResponseDto> _expander;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly MapsterMapper.IMapper _mapsterMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public ZakenController(
        ILogger<ZakenController> logger,
        IMediator mediator,
        AutoMapper.IMapper mapper,
        MapsterMapper.IMapper mapsterMapper,
        IRequestMerger requestMerger, // unused here; ZGWControllerBase's constructor still requires it
        IZgwRequestMerger zgwRequestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService,
        IErrorResponseBuilder errorResponseBuilder,
        IExpanderFactory expanderFactory
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _zgwRequestMerger = zgwRequestMerger;
        _mapsterMapper = mapsterMapper;
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expander = expanderFactory.Create<ZaakResponseDto>("zaak");
    }

    /* Tests with expand...
        http://zaken.user.local:5005/api/v1/zaken/8a3fb51a-9741-4552-b7e3-29896b3ffbfa?expand=zaaktype,catalogus,status,status.statustype,resultaat,resultaat.resultaattype
        http://zaken.user.local:5005/api/v1/zaken?expand=status,resultaat
        http://zaken.user.local:5005/api/v1/zaken?expand=status,status.statustype,resultaat,resultaat.resultaattype
        http://zaken.user.local:5005/api/v1/zaken/8a3fb51a-9741-4552-b7e3-29896b3ffbfa?expand=hoofdzaak.deelzaken.status
        http://zaken.user.local:5005/api/v1/zaken/cfb1a856-d033-414c-b1ed-c5d73f9d8fe0?expand=hoofdzaak.status.statustype
    */

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
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllZakenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZakenQueryParameters queryParameters, int page = 1, string ordering = null)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}, {Ordering}", nameof(GetAllAsync), queryParameters, page, ordering);

        var pagination = _mapsterMapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));
        var filter = _mapsterMapper.Map<GetAllZakenFilter>(queryParameters);

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

        var zakenResponse = _mapsterMapper.Map<List<ZaakResponseDto>>(result.Result.PageResult);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var zakenWithOptionalExpand = zakenResponse.Select(z => _expander.ResolveAsync(expandLookup, z).Result).ToList();

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zakenWithOptionalExpand, result.Result.Count);

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
    [HttpPost(ApiRoutes.Zaken.Search, Name = Operations.Zaken.Search)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakResponseDto>))]
    [RequiresAcceptCrs]
    [Expand]
    [ServiceFilter(typeof(ValidateBodyParametersFilter<ZaakSearchRequestDto>))]
    public async Task<IActionResult> SearchAsync([FromBody] ZaakSearchRequestDto zaakSearchRequest, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Page}", nameof(SearchAsync), zaakSearchRequest, page);

        var pagination = _mapsterMapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZakenPageSize));
        var filter = _mapsterMapper.Map<GetAllZakenFilter>(zaakSearchRequest);

        var result = await _mediator.Send(
            new GetAllZakenQuery
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

        var zakenResponse = _mapsterMapper.Map<List<ZaakResponseDto>>(result.Result.PageResult);

        var expandLookup = ExpandLookup(zaakSearchRequest.Expand);

        var zakenWithOptionalExpand = zakenResponse.Select(z => _expander.ResolveAsync(expandLookup, z).Result).ToList();

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(pagination, zakenWithOptionalExpand, result.Result.Count);

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
    /// Beschikbare snapshots van de ZAAK-collectie opvragen (synchronisatie van collecties).
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Zaken.Snapshots, Name = Operations.Zaken.Snapshots)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakSnapshotsResponseDto))]
    public async Task<IActionResult> GetSnapshotsAsync([FromQuery] GetZaakSnapshotsQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetSnapshotsAsync), queryParameters);

        var result = await _mediator.Send(
            new GetZaakSnapshotsQuery
            {
                After = queryParameters.After,
                Limit = queryParameters.Limit,
                AanmaakdatumGte = queryParameters.AanmaakdatumGte,
            }
        );

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var items = _mapsterMapper.Map<List<ZaakSnapshotDto>>(result.Result);

        if (!IncludeNonStandardSnapshotFields)
        {
            foreach (var item in items)
            {
                item.Resource = null;
                item.ResourceId = null;
            }
        }

        var response = new ZaakSnapshotsResponseDto { Items = items };

        return Ok(response);
    }

    /// <summary>
    /// Een individuele snapshot opvragen (de href uit een _snapshots-item).
    /// </summary>
    /// <remarks>
    /// Niet onderdeel van de VNG-standaard "Synchronisatie van collecties" — die specificeert geen
    /// vorm voor het ophalen van een individuele snapshot, maar elk _snapshots-item heeft een href die
    /// hierheen wijst, dus dit endpoint maakt die href werkend.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Zaken.SnapshotGet, Name = Operations.Zaken.SnapshotRead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakSnapshotDetailDto))]
    public async Task<IActionResult> GetSnapshotAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetSnapshotAsync), id);

        var result = await _mediator.Send(new GetZaakSnapshotQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var snapshot = result.Result;

        var response = new ZaakSnapshotDetailDto
        {
            Id = snapshot.Id.ToString(),
            ResourceId = snapshot.ResourceId?.ToString(),
            Resource = snapshot.SnapshotJson != null ? JToken.Parse(snapshot.SnapshotJson) : null,
        };

        return Ok(response);
    }

    /// <summary>
    /// Wijzigingen (delta's) op één ZAAK opvragen sinds een gegeven state-id (synchronisatie van collecties).
    /// </summary>
    /// <remarks>
    /// <c>after</c> is verplicht: dit is zowel het startpunt in de tijd als de identificatie van de ZAAK
    /// (via de audittrail-rij die dat id representeert, bv. uit <c>_snapshots</c>) — geen collectie-brede
    /// stream, maar het vervolg van de versie-geschiedenis van die ene zaak.
    /// Ondersteunt zowel gewone JSON-polling (via de <c>after</c>/<c>limit</c> query-parameters) als een SSE-variant
    /// op dezelfde resource, content-genegotieerd via de <c>Accept: text/event-stream</c> header.
    /// </remarks>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Zaken.Deltas, Name = Operations.Zaken.Deltas)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakDeltasResponseDto))]
    public async Task<IActionResult> GetDeltasAsync([FromQuery] GetZaakDeltasQueryParameters queryParameters)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}", nameof(GetDeltasAsync), queryParameters);

        if (Request.Headers.Accept.Any(accept => accept != null && accept.Contains("text/event-stream")))
        {
            Response.ContentType = "text/event-stream";

            throw new NotImplementedException();
        }

        var result = await _mediator.Send(new GetZaakDeltasQuery { After = queryParameters.After, Limit = queryParameters.Limit });

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        // 'id' is de echte primary key van de audittrail_deltas-rij (dezelfde id als bij _snapshots),
        // geen berekende positie. 'prev_id' is de id van de voorganger in dezelfde (AanmaakDatum, Id)-
        // ordening binnen dezelfde resource als 'after': voor het eerste item in de pagina is dat
        // 'after' zelf (gap-detectie voor de consumer), voor de rest gewoon de vorige rij in deze pagina.
        var pageResult = result.Result.PageResult.ToList();

        var deltas = pageResult
            .Select(
                (delta, index) =>
                {
                    string prevId = index == 0 ? queryParameters.After.Value.ToString() : pageResult[index - 1].Id.ToString();
                    return MapToDeltaDto(delta, prevId);
                }
            )
            .ToList();

        var response = new ZaakDeltasResponseDto { Items = deltas };

        return Ok(response);
    }

    private static ZaakDeltaDto MapToDeltaDto(AuditTrailDelta delta, string prevId)
    {
        // Note: create/retrieve slaan de volledige resource-state op in SnapshotJson, update/partial_update
        // meestal in DeltaJson (een gegenereerde diff, behalve op geforceerde snapshot-versies), en destroy
        // slaat de laatst-bekende state juist onder DeltaJson op (zie ResolveSnapshotOrDeltaAsync).
        var resourceJson = delta.SnapshotJson ?? delta.DeltaJson;

        return new ZaakDeltaDto
        {
            Id = delta.Id.ToString(),
            PrevId = prevId,
            Operations = new List<ZaakDeltaOperationDto>
            {
                new ZaakDeltaOperationDto
                {
                    Type = MapActieToOperationType(delta.Actie),
                    ResourceId = delta.ResourceId?.ToString(),
                    Resource = resourceJson != null ? JToken.Parse(resourceJson) : null,
                },
            },
        };
    }

    // Vertaalt onze interne audittrail-actie naar de generieke CRUD-woordenschat uit het
    // synchronisatiepatroon (create/update/delete) — 'partial_update' en 'destroy' zijn interne
    // ZGW-termen die niet als zodanig in de publieke _deltas-contract horen te lekken.
    private static string MapActieToOperationType(string actie) =>
        actie switch
        {
            nameof(AuditActie.partial_update) => nameof(AuditActie.update),
            nameof(AuditActie.destroy) => "delete",
            _ => actie,
        };

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
    public async Task<IActionResult> AddAsync([FromBody] ZaakRequestDto zaakRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Rsin}", nameof(AddAsync), zaakRequest, zaakRequest.Bronorganisatie);

        var zaak = _mapsterMapper.Map<Zaak>(zaakRequest);

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

        var zaakResponse = _mapsterMapper.Map<ZaakResponseDto>(result.Result);

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
    [HttpGet(ApiRoutes.Zaken.Get, Name = Operations.Zaken.Read)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [RequiresAcceptCrs]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync([FromQuery] GetZaakQueryParameters queryParameters, Guid id)
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

        var zaak = _mapsterMapper.Map<ZaakResponseDto>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var zaakWithOptionalExpand = await _expander.ResolveAsync(expandLookup, zaak);

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

        return Ok(zaakWithOptionalExpand);
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
    [HttpHead(ApiRoutes.Zaken.Get, Name = Operations.Zaken.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetZaakQueryParameters queryParameters)
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
    [HttpPut(ApiRoutes.Zaken.Update, Name = Operations.Zaken.Update)]
    [Scope(AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakResponseDto))]
    [RequiresContentCrs, RequiresAcceptCrs]
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

        var zaak = _mapsterMapper.Map<Zaak>(zaakRequest);

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

        var zaakResponse = _mapsterMapper.Map<ZaakResponseDto>(result.Result);

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

        var mergedZaakRequest = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ZaakRequestDto, Zaak>(resultGet.Result, partialZaakRequest);

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

        var mergedZaak = _mapsterMapper.Map<Zaak>(mergedZaakRequest);

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

        var zaakResponse = _mapsterMapper.Map<ZaakResponseDto>(resultUpd.Result);

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
    [HttpGet(ApiRoutes.ZaakEigenschappen.Get, Name = Operations.ZaakEigenschappen.Read)]
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

        var response = _mapsterMapper.Map<Zaken.Contracts.v1.Responses.ZaakEigenschapResponseDto>(result.Result);

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
    [HttpHead(ApiRoutes.ZaakEigenschappen.Get, Name = Operations.ZaakEigenschappen.ReadHead)]
    [Scope(AuthorizationScopes.Zaken.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadZaakEigenschapAsync(Guid zaak_uuid, Guid uuid)
    {
        return GetZaakEigenschapAsync(zaak_uuid, uuid);
    }
}
