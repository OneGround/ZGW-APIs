using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.MimeTypes;
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
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Configuration;
using OneGround.ZGW.Documenten.Web.Contracts.v1._5;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5;
using OneGround.ZGW.Documenten.Web.Models.v1._5;
using Swashbuckle.AspNetCore.Annotations;

// DRC large files: https://vng-realisatie.github.io/gemma-zaken/ontwikkelaars/handleidingen-en-tutorials/large-files

namespace OneGround.ZGW.Documenten.Web.Controllers.v1._5;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_5)]
[Consumes("application/json")]
[Produces("application/json")]
public class EnkelvoudigInformatieObjectenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IObjectExpander<EnkelvoudigInformatieObjectGetResponseDto> _expander;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public EnkelvoudigInformatieObjectenController(
        ILogger<EnkelvoudigInformatieObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        IValidatorService validatorService,
        IExpanderFactory expanderFactory
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _expander = expanderFactory.Create<EnkelvoudigInformatieObjectGetResponseDto>("enkelvoudiginformatieobject");
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten

    /// <summary>
    /// Alle ENKELVOUDIGEINFORMATIEOBJECTen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.GetAll, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<EnkelvoudigInformatieObjectGetResponseExpandedDto>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] GetAllEnkelvoudigInformatieObjectenQueryParameters queryParameters,
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.EnkelvoudigInformatieObjectenPageSize));
        var filter = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllEnkelvoudigInformatieObjectenQuery { GetAllEnkelvoudigInformatieObjectenFilter = filter, Pagination = pagination },
            cancellationToken
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var enkelvoudigInformatieObjectenResponse = _mapper.Map<List<EnkelvoudigInformatieObjectGetResponseDto>>(result.Result.PageResult);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var enkelvoudigInformatieObjectenWithOptionalExpand = enkelvoudigInformatieObjectenResponse
            .Select(e => _expander.ResolveAsync(expandLookup, e).Result)
            .ToList();

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            enkelvoudigInformatieObjectenWithOptionalExpand,
            result.Result.Count
        );

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" },
            },
            cancellationToken
        );

        return Ok(paginationResponse);
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09?versie=2&registratieop=2020-11-07

    /// <summary>
    /// Een specifieke ENKELVOUDIGEINFORMATIEOBJECT opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.Get, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectGetResponseExpandedDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromQuery] GetEnkelvoudigInformatieObjectQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(GetAsync), id, queryParameters);

        var filter = _mapper.Map<Models.v1.GetEnkelvoudigInformatieObjectFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetEnkelvoudigInformatieObjectQuery { Id = id, GetEnkelvoudigInformatieObjectFilter = filter },
            cancellationToken
        );

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var enkelvoudigInformatieObject = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var enkelvoudigInformatieObjectWithOptionalExpand = await _expander.ResolveAsync(expandLookup, enkelvoudigInformatieObject);

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                BaseEntity = result.Result,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" },
            },
            cancellationToken
        );

        return Ok(enkelvoudigInformatieObjectWithOptionalExpand);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ENKELVOUDIG INFORMATIE OBJECT opvragen.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.EnkelvoudigInformatieObjecten.Get, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(
        Guid id,
        [FromQuery] GetEnkelvoudigInformatieObjectQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        return GetAsync(id, queryParameters, cancellationToken);
    }

    //
    // HTTP POST http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/_zoek

    /// <summary>
    /// Voer een zoekopdracht uit op (ENKELVOUDIG) INFORMATIEOBJECTen.
    /// </summary>
    /// <remarks>
    /// Zoeken/filteren gaat normaal via de list operatie, deze is echter niet geschikt voor zoekopdrachten met UUIDs.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Search, Name = Operations.EnkelvoudigInformatieObjecten.Search)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<EnkelvoudigInformatieObjectGetResponseExpandedDto>))]
    [Expand]
    public async Task<IActionResult> SearchAsync(
        [FromBody] EnkelvoudigInformatieObjectSearchRequestDto enkelvoudiginformatieobjectSearchRequest,
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Page}", nameof(SearchAsync), enkelvoudiginformatieobjectSearchRequest, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.EnkelvoudigInformatieObjectenPageSize));
        var filter = _mapper.Map<GetAllEnkelvoudigInformatieObjectenFilter>(enkelvoudiginformatieobjectSearchRequest);

        var result = await _mediator.Send(
            new GetAllEnkelvoudigInformatieObjectenQuery { GetAllEnkelvoudigInformatieObjectenFilter = filter, Pagination = pagination },
            cancellationToken
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var enkelvoudigInformatieObjectenResponse = _mapper.Map<List<EnkelvoudigInformatieObjectGetResponseDto>>(result.Result.PageResult);

        var expandLookup = ExpandLookup(enkelvoudiginformatieobjectSearchRequest.Expand);

        var enkelvoudigInformatieObjectenWithOptionalExpand = enkelvoudigInformatieObjectenResponse
            .Select(e => _expander.ResolveAsync(expandLookup, e).Result)
            .ToList();

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            pagination,
            enkelvoudigInformatieObjectenWithOptionalExpand,
            result.Result.Count
        );

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" },
            },
            cancellationToken
        );

        return Ok(paginationResponse);
    }

    //
    // HTTP POST http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten

    /// <summary>
    /// Maak een (ENKELVOUDIG) INFORMATIEOBJECT aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Create, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Create)]
    [Scope(AuthorizationScopes.Documenten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(EnkelvoudigInformatieObjectCreateResponseDto))]
    public async Task<IActionResult> AddAsync(
        [FromBody] EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObjectRequest,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {@FromBody}, {Rsin}",
            nameof(AddAsync),
            enkelvoudigInformatieObjectRequest,
            enkelvoudigInformatieObjectRequest.Bronorganisatie
        );

        var enkelvoudigInformatieObjectVersie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(enkelvoudigInformatieObjectRequest);

        // Note: we should investigate who send the 2-letter language code so we log for these situations
        LogInvalidTaalCode(enkelvoudigInformatieObjectRequest.Taal, enkelvoudigInformatieObjectVersie.Taal);

        var result = await _mediator.Send(
            new CreateEnkelvoudigInformatieObjectCommand { EnkelvoudigInformatieObjectVersie = enkelvoudigInformatieObjectVersie },
            cancellationToken
        );

        if (result.Status == CommandStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var enkelvoudigInformatieObjectResponse = _mapper.Map<EnkelvoudigInformatieObjectCreateResponseDto>(result.Result);

        return Created(enkelvoudigInformatieObjectResponse.Url, enkelvoudigInformatieObjectResponse);
    }

    //
    // HTTP PUT http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b

    /// <summary>
    /// Werk een (ENKELVOUDIG) INFORMATIEOBJECT in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.EnkelvoudigInformatieObjecten.Update, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Update)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectUpdateResponseDto))]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] EnkelvoudigInformatieObjectUpdateRequestDto enkelvoudigInformatieObjectRequest,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {@FromBody}, {Rsin}",
            nameof(UpdateAsync),
            enkelvoudigInformatieObjectRequest,
            enkelvoudigInformatieObjectRequest.Bronorganisatie
        );

        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(
            enkelvoudigInformatieObjectRequest
        );

        // Note: we should investigate who send the 2-letter language code so we log for these situations
        LogInvalidTaalCode(enkelvoudigInformatieObjectRequest.Taal, enkelvoudigInformatieObjectVersie.Taal);

        var result = await _mediator.Send(
            new UpdateEnkelvoudigInformatieObjectCommand
            {
                ExistingEnkelvoudigInformatieObjectId = id,
                EnkelvoudigInformatieObjectVersie = enkelvoudigInformatieObjectVersie, // Note: Indicates that the versie should be fully replaced in the command handler
                MergeWithPartial = null,
            },
            cancellationToken
        );

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

        if (result.Status == CommandStatus.Conflict)
        {
            return _errorResponseBuilder.Conflict(result.Errors);
        }

        var enkelvoudigInformatieObjectResponse = _mapper.Map<EnkelvoudigInformatieObjectUpdateResponseDto>(result.Result);

        return Ok(enkelvoudigInformatieObjectResponse);
    }

    //
    // HTTP PATCH http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b

    /// <summary>
    /// Werk een (ENKELVOUDIG) INFORMATIEOBJECT deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.EnkelvoudigInformatieObjecten.Update, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.PartialUpdate)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectUpdateResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync(
        [FromBody] dynamic partialEnkelvoudigInformatieObjectRequest,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        // We do log only the request not the partial update request (because can be large)
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var result = await _mediator.Send(
            new UpdateEnkelvoudigInformatieObjectCommand
            {
                ExistingEnkelvoudigInformatieObjectId = id,
                EnkelvoudigInformatieObjectVersie = null, // Note: Indicates that the versie should be merged in the command handler
                MergeWithPartial = (eoi) => TryMergeWithRequestBody(partialEnkelvoudigInformatieObjectRequest, eoi),
            },
            cancellationToken
        );

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        if (result.Status == CommandStatus.Conflict)
        {
            return _errorResponseBuilder.Conflict(result.Errors);
        }

        var enkelvoudigInformatieObjectResponse = _mapper.Map<EnkelvoudigInformatieObjectUpdateResponseDto>(result.Result);

        return Ok(enkelvoudigInformatieObjectResponse);
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09/download
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09/download?versie=2

    /// <summary>
    /// Download de binaire data van het (ENKELVOUDIG) INFORMATIEOBJECT..
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.Download, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Download)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadAsync(
        Guid id,
        [FromQuery] DownloadEnkelvoudigInformatieObjectQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(DownloadAsync), id, queryParameters);

        var filter = _mapper.Map<Models.v1.GetEnkelvoudigInformatieObjectFilter>(queryParameters);

        var resultGet = await _mediator.Send(
            new GetEnkelvoudigInformatieObjectQuery
            {
                Id = id,
                GetEnkelvoudigInformatieObjectFilter = filter,
                IgnoreNotCompletedDocuments = true,
            },
            cancellationToken
        );

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var enkelvoudigInformatieObjectVersie = resultGet.Result.EnkelvoudigInformatieObjectVersies.Single();

        // Note: New in v1.1: if file size = 0, i.e.EnkelvoudigInformatieObject contains only metadata without file content.The EnkelvoudigInformatieObject is created using a single request to Documenten API.
        if (enkelvoudigInformatieObjectVersie.Bestandsomvang == 0 && enkelvoudigInformatieObjectVersie.Inhoud == null)
        {
            if (_applicationConfiguration.DocumentJobPrioritizationAtDownload)
            {
                await _mediator.Send(
                    new Handlers.v1._1.PrioritizationDocumentJobCommand
                    {
                        EnkelvoudigInformatieObjectId = enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObjectId,
                    },
                    cancellationToken
                );
            }
            // Document meta does exists but no content
            return NoContent(); // TODO: Unclear what to return. ZGW reference does respond with HTTP Status 500. I think 204 [NoContent] make sense here
        }

        var documentUrn = new DocumentUrn(enkelvoudigInformatieObjectVersie.Inhoud);

        var resultDwnl = await _mediator.Send(
            new DownloadEnkelvoudigInformatieObjectQuery
            {
                DocumentUrn = documentUrn,
                EnkelvoudigInformatieObjectId = enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObjectId,
            },
            cancellationToken
        );

        if (resultDwnl.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                BaseEntity = resultGet.Result,
                SubEntity = enkelvoudigInformatieObjectVersie,
                OverruleActieWeergave = "Object gedownload",
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" },
            },
            cancellationToken
        );

        var cd = new ContentDisposition { FileName = HttpUtility.UrlPathEncode(enkelvoudigInformatieObjectVersie.Bestandsnaam) };

        Response.Headers.ContentDisposition = cd.ToString();
        Response.Headers.XContentTypeOptions = "nosniff";

        var mimeType = string.IsNullOrEmpty(enkelvoudigInformatieObjectVersie.Formaat)
            ? MimeTypeHelper.GetMimeType(enkelvoudigInformatieObjectVersie.Bestandsnaam)
            : enkelvoudigInformatieObjectVersie.Formaat;

        return File(resultDwnl.Result, mimeType);
    }

    /// <summary>
    /// Vergrendel een (ENKELVOUDIG) INFORMATIEOBJECT.
    /// Voert een "checkout" uit waardoor het (ENKELVOUDIG) INFORMATIEOBJECT vergrendeld wordt met een lock waarde.
    /// Alleen met deze waarde kan het (ENKELVOUDIG) INFORMATIEOBJECT bijgewerkt (PUT, PATCH) en weer ontgrendeld worden.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Lock, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Lock)]
    [Scope(AuthorizationScopes.Documenten.Lock)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Documenten.Contracts.v1.Responses.LockResponseDto))]
    public async Task<IActionResult> LockAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LockEnkelvoudigInformatieObjectCommand { Id = id, Set = true }, cancellationToken);

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

        if (result.Status == CommandStatus.Conflict)
        {
            return _errorResponseBuilder.Conflict(result.Errors);
        }

        var response = new Documenten.Contracts.v1.Responses.LockResponseDto { Lock = result.Result };

        return Ok(response);
    }

    //
    // HTTP POST https://documenten-api.vng.cloud/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09/unlock

    /// <summary>
    /// Ontgrendel een (ENKELVOUDIG) INFORMATIEOBJECT.
    /// Heft de "checkout" op waardoor het (ENKELVOUDIG) INFORMATIEOBJECT ontgrendeld wordt.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Unlock, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Unlock)]
    [Scope(AuthorizationScopes.Documenten.Lock, AuthorizationScopes.Documenten.ForcedUnlock)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [IgnoreMissingContentType]
    public async Task<IActionResult> UnlockAsync(
        Guid id,
        [FromBody] Documenten.Contracts.v1.Requests.LockRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _mediator.Send(
            new LockEnkelvoudigInformatieObjectCommand
            {
                Id = id,
                Set = false,
                Lock = request?.Lock,
            },
            cancellationToken
        );

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

        if (result.Status == CommandStatus.Conflict)
        {
            return _errorResponseBuilder.Conflict(result.Errors);
        }

        return NoContent();
    }

    private (EnkelvoudigInformatieObjectVersie versie, IList<ValidationError> errors) TryMergeWithRequestBody(
        dynamic partialEnkelvoudigInformatieObjectRequest,
        EnkelvoudigInformatieObject enkelvoudigInformatieObject
    )
    {
        EnkelvoudigInformatieObjectUpdateRequestDto mergedEnkelvoudigInformatieObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            EnkelvoudigInformatieObjectUpdateRequestDto,
            EnkelvoudigInformatieObject
        >(enkelvoudigInformatieObject, partialEnkelvoudigInformatieObjectRequest);

        if (!_validatorService.IsValid(mergedEnkelvoudigInformatieObjectRequest, out var validationResult))
        {
            return (versie: null, errors: validationResult.ToValidationErrors());
        }

        var enkelvoudigInformatieObjectVersie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(mergedEnkelvoudigInformatieObjectRequest);

        // Note: we should investigate who send the 2-letter language code so we log for these situations
        LogInvalidTaalCode(mergedEnkelvoudigInformatieObjectRequest.Taal, enkelvoudigInformatieObjectVersie.Taal);

        return (versie: enkelvoudigInformatieObjectVersie, errors: null);
    }
}
