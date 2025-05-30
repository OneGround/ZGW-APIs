﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
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
using Roxit.ZGW.Common.MimeTypes;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Filters;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Middleware;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Versioning;
using Roxit.ZGW.Documenten.Contracts.v1._1.Requests;
using Roxit.ZGW.Documenten.Contracts.v1._1.Responses;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Services;
using Roxit.ZGW.Documenten.Web.Authorization;
using Roxit.ZGW.Documenten.Web.Configuration;
using Roxit.ZGW.Documenten.Web.Contracts.v1._1;
using Roxit.ZGW.Documenten.Web.Handlers.v1._1;
using Swashbuckle.AspNetCore.Annotations;

// DRC large files: https://vng-realisatie.github.io/gemma-zaken/ontwikkelaars/handleidingen-en-tutorials/large-files

namespace Roxit.ZGW.Documenten.Web.Controllers.v1._1;

[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_1)]
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
    [HttpGet(ApiRoutes.EnkelvoudigInformatieObjecten.GetAll, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<EnkelvoudigInformatieObjectGetResponseDto>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Documenten.Contracts.v1.Queries.GetAllEnkelvoudigInformatieObjectenQueryParameters queryParameters,
        int page = 1
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.EnkelvoudigInformatieObjectenPageSize));
        var filter = _mapper.Map<Models.v1.GetAllEnkelvoudigInformatieObjectenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllEnkelvoudigInformatieObjectenQuery { GetAllEnkelvoudigInformatieObjectenFilter = filter, Pagination = pagination }
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
            }
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
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectGetResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromQuery] Documenten.Contracts.v1.Queries.GetEnkelvoudigInformatieObjectQueryParameters queryParameters
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(GetAsync), id, queryParameters);

        var filter = _mapper.Map<Models.v1.GetEnkelvoudigInformatieObjectFilter>(queryParameters);

        var result = await _mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = id, GetEnkelvoudigInformatieObjectFilter = filter });

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
            }
        );

        return Ok(enkelvoudigInformatieObjectResponse);
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
    public Task<IActionResult> HeadAsync(
        Guid id,
        [FromQuery] Documenten.Contracts.v1.Queries.GetEnkelvoudigInformatieObjectQueryParameters queryParameters
    )
    {
        return GetAsync(id, queryParameters);
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
    public async Task<IActionResult> AddAsync([FromBody] EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObjectRequest)
    {
        _logger.LogDebug(
            "{ControllerMethod} called with {@FromBody}, {Rsin}",
            nameof(AddAsync),
            enkelvoudigInformatieObjectRequest,
            enkelvoudigInformatieObjectRequest.Bronorganisatie
        );

        var enkelvoudigInformatieObjectVersie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(enkelvoudigInformatieObjectRequest);

        var result = await _mediator.Send(
            new CreateEnkelvoudigInformatieObjectCommand { EnkelvoudigInformatieObjectVersie = enkelvoudigInformatieObjectVersie }
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
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.EnkelvoudigInformatieObjecten.Update, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Update)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectUpdateResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] EnkelvoudigInformatieObjectUpdateRequestDto enkelvoudigInformatieObjectRequest, Guid id)
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

        var result = await _mediator.Send(
            new UpdateEnkelvoudigInformatieObjectCommand
            {
                ExistingEnkelvoudigInformatieObjectId = id,
                EnkelvoudigInformatieObjectVersie = enkelvoudigInformatieObjectVersie,
                IsPartialUpdate = false,
            }
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
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.EnkelvoudigInformatieObjecten.Update, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.PartialUpdate)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(EnkelvoudigInformatieObjectUpdateResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] dynamic partialEnkelvoudigInformatieObjectRequest, Guid id)
    {
        // We do log only the request not the partial update request (because can be large)
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = id, IgnoreLock = true });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        EnkelvoudigInformatieObjectUpdateRequestDto mergedEnkelvoudigInformatieObjectRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            EnkelvoudigInformatieObjectUpdateRequestDto,
            EnkelvoudigInformatieObject
        >(resultGet.Result, partialEnkelvoudigInformatieObjectRequest);

        if (!_validatorService.IsValid(mergedEnkelvoudigInformatieObjectRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        EnkelvoudigInformatieObjectVersie enkelvoudigInformatieObjectVersie = _mapper.Map<EnkelvoudigInformatieObjectVersie>(
            mergedEnkelvoudigInformatieObjectRequest
        );

        var result = await _mediator.Send(
            new UpdateEnkelvoudigInformatieObjectCommand
            {
                ExistingEnkelvoudigInformatieObjectId = id,
                EnkelvoudigInformatieObjectVersie = enkelvoudigInformatieObjectVersie,
                IsPartialUpdate = true,
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
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadAsync(
        Guid id,
        [FromQuery] Documenten.Contracts.v1.Queries.GetEnkelvoudigInformatieObjectQueryParameters queryParameters
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
            }
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
                    new PrioritizationDocumentJobCommand
                    {
                        EnkelvoudigInformatieObjectId = enkelvoudigInformatieObjectVersie.EnkelvoudigInformatieObjectId,
                    }
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
            }
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
            }
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
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Lock, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Lock)]
    [Scope(AuthorizationScopes.Documenten.Lock)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Documenten.Contracts.v1.Responses.LockResponseDto))]
    public async Task<IActionResult> LockAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(LockAsync), id);

        var result = await _mediator.Send(new LockEnkelvoudigInformatieObjectCommand { Id = id, Set = true });

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
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.EnkelvoudigInformatieObjecten.Unlock, Name = Contracts.v1.Operations.EnkelvoudigInformatieObjecten.Unlock)]
    [Scope(AuthorizationScopes.Documenten.Lock, AuthorizationScopes.Documenten.ForcedUnlock)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [IgnoreMissingContentType]
    public async Task<IActionResult> UnlockAsync(Guid id, [FromBody] Documenten.Contracts.v1.Requests.LockRequestDto request)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromBody}", nameof(UnlockAsync), id, request);

        var result = await _mediator.Send(
            new LockEnkelvoudigInformatieObjectCommand
            {
                Id = id,
                Set = false,
                Lock = request?.Lock,
            }
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

        return NoContent();
    }
}
