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
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.MimeTypes;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Middleware;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Configuration;
using OneGround.ZGW.Documenten.Web.Contracts.v1;
using OneGround.ZGW.Documenten.Web.Handlers.v1;
using OneGround.ZGW.Documenten.Web.Models.v1;
using Swashbuckle.AspNetCore.Annotations;

//
// Bron DRC API: https://documenten-api.vng.cloud/api/v1/schema/

namespace OneGround.ZGW.Documenten.Web.Controllers.v1;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_0)]
[Consumes("application/json")]
[Produces("application/json")]
public class EnkelvoudigInformatieObjectenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public EnkelvoudigInformatieObjectenController(
        ILogger<EnkelvoudigInformatieObjectenController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        IValidatorService validatorService
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
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
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.GetAll, Name = Operations.EnkelvoudigInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<EnkelvoudigInformatieObjectGetResponseDto>))]
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

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            enkelvoudigInformatieObjectenResponse,
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
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.Get, Name = Operations.EnkelvoudigInformatieObjecten.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectGetResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromQuery] GetEnkelvoudigInformatieObjectQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(GetAsync), id, queryParameters);

        var filter = _mapper.Map<GetEnkelvoudigInformatieObjectFilter>(queryParameters);

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

        var enkelvoudigInformatieObjectResponse = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);

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

        return Ok(enkelvoudigInformatieObjectResponse);
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
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Create, Name = Operations.EnkelvoudigInformatieObjecten.Create)]
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
    [HttpPut(ApiRoutes.EnkelvoudigInformatieObjecten.Update, Name = Operations.EnkelvoudigInformatieObjecten.Update)]
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
            new CreateEnkelvoudigInformatieObjectCommand
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

    /// <summary>
    /// Werk een (ENKELVOUDIG) INFORMATIEOBJECT deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.EnkelvoudigInformatieObjecten.Update, Name = Operations.EnkelvoudigInformatieObjecten.PartialUpdate)]
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
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var result = await _mediator.Send(
            new CreateEnkelvoudigInformatieObjectCommand
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
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.Download, Name = Operations.EnkelvoudigInformatieObjecten.Download)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadAsync(
        Guid id,
        [FromQuery] GetEnkelvoudigInformatieObjectQueryParameters queryParameters,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(DownloadAsync), id, queryParameters);

        var filter = _mapper.Map<GetEnkelvoudigInformatieObjectFilter>(queryParameters);

        var resultGet = await _mediator.Send(
            new GetEnkelvoudigInformatieObjectQuery { Id = id, GetEnkelvoudigInformatieObjectFilter = filter },
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

        // Note: When a v1.1 multi-part document is uploaded (and not completed yet) the
        //       download under v1 must be handled with care because inhoud field is null so an extra check is needed here!
        //   or:
        //       When a 1.1 mata-only document is added an retrieved by a 1.0 version
        if (enkelvoudigInformatieObjectVersie.Inhoud == null)
        {
            return _errorResponseBuilder.NotFound();
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

    //
    // HTTP POST https://documenten-api.vng.cloud/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09/lock

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
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Lock, Name = Operations.EnkelvoudigInformatieObjecten.Lock)]
    [Scope(AuthorizationScopes.Documenten.Lock)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(LockResponseDto))]
    public async Task<IActionResult> LockAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(LockAsync), id);

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

        var response = new LockResponseDto { Lock = result.Result };

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
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Unlock, Name = Operations.EnkelvoudigInformatieObjecten.Unlock)]
    [Scope(AuthorizationScopes.Documenten.Lock, AuthorizationScopes.Documenten.ForcedUnlock)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [IgnoreMissingContentType]
    public async Task<IActionResult> UnlockAsync(Guid id, [FromBody] LockRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromBody}", nameof(UnlockAsync), id, request);

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

    //
    // HTTP DELETE https://documenten-api.vng.cloud/api/v1/enkelvoudiginformatieobjecten/b24ee37c-00db-4108-b831-e3b420b35a09

    /// <summary>
    /// Verwijder een (ENKELVOUDIG) INFORMATIEOBJECT.
    /// Verwijder een(ENKELVOUDIG) INFORMATIEOBJECT en alle bijbehorende versies, samen met alle gerelateerde resources binnen deze API.
    /// Dit is alleen mogelijk als er geen OBJECTINFORMATIEOBJECTen relateerd zijn aan het (ENKELVOUDIG) INFORMATIEOBJECT.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="409">EnkelvoudigInformatieObject was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.EnkelvoudigInformatieObjecten.Delete, Name = Operations.EnkelvoudigInformatieObjecten.Delete)]
    [Scope(AuthorizationScopes.Documenten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteEnkelvoudigInformatieObjectCommand { Id = id }, cancellationToken);

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

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b/audittrail

    /// <summary>
    /// Alle audit trail regels behorend bij het INFORMATIEOBJECT
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjectAudittrail.GetAll, Name = Operations.EnkelvoudigInformatieObjectAudittrail.List)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(List<AuditTrailRegelDto>))]
    public async Task<IActionResult> GetAllAuditTrailRegelsAsync(Guid enkelvoudiginformatieobject_uuid, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {EnkelvoudigInformatieObjectUuid}",
            nameof(GetAllAuditTrailRegelsAsync),
            enkelvoudiginformatieobject_uuid
        );

        var result = await _mediator.Send(
            new GetAllEnkelvoudigInformatieObjectAuditTrailRegels { EnkelvoudigInformatieObjectId = enkelvoudiginformatieobject_uuid },
            cancellationToken
        );

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var enkelvoudiginformatieobjectAuditTrailRegelsResponse = _mapper.Map<List<AuditTrailRegelDto>>(result.Result);

        return Ok(enkelvoudiginformatieobjectAuditTrailRegelsResponse);
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/enkelvoudiginformatieobjecten/59bad509-840b-4cd0-82dc-cbda74a75c2b/audittrail/782b4144-0185-4180-8b59-2ce322dad69d

    /// <summary>
    /// Een specifieke audit trail regel opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjectAudittrail.Get, Name = Operations.EnkelvoudigInformatieObjectAudittrail.Read)]
    [Scope(AuthorizationScopes.AuditTrails.Read)]
    [ZgwApiVersion(Api.LatestVersion_1_0)]
    [ZgwApiVersion(Api.LatestVersion_1_1)]
    [ZgwApiVersion(Api.LatestVersion_1_5)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(AuditTrailRegelDto))]
    public async Task<IActionResult> GetAuditTrailRegelAsync(Guid enkelvoudiginformatieobject_uuid, Guid uuid, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {EnkelvoudigInformatieObjectUuid}, {Uuid}",
            nameof(GetAuditTrailRegelAsync),
            enkelvoudiginformatieobject_uuid,
            uuid
        );

        var result = await _mediator.Send(
            new GetEnkelvoudigInformatieObjectAuditTrailRegel
            {
                EnkelvoudigInformatieObjectId = enkelvoudiginformatieobject_uuid,
                AuditTrailRegelId = uuid,
            },
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

        var enkelvoudiginformatieobjectAuditTrailRegelResponse = _mapper.Map<AuditTrailRegelDto>(result.Result);

        return Ok(enkelvoudiginformatieobjectAuditTrailRegelResponse);
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
