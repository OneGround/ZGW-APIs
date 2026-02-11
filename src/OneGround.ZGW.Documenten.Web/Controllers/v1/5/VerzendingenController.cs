using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Configuration;
using OneGround.ZGW.Documenten.Web.Contracts.v1._5;
using OneGround.ZGW.Documenten.Web.Expands.v1._5;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5;
using OneGround.ZGW.Documenten.Web.Models.v1._5;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Documenten.Web.Controllers.v1._5;

/*
    https://vng-realisatie.github.io/gemma-zaken/standaard/documenten/

    Verzending
    Nieuw in versie 1.2.0 De relatie klasse Verzending legt vast aan welke Betrokkene een Informatieobject verzonden is of
    van welke Betrokkene een Informatieobject ontvangen is. Om altijd te kunnen achterhalen naar/van welk adres een
    Informatieobject verzonden of ontvangen is moet dit adres ook worden vastgelegd. Immers, wanneer alleen verwezen wordt
    naar het adres waarop iemand ingeschreven staat verandert dit gegeven wanneer deze persoon verhuist of de geregistreerde
    gegevens bijgewerkt worden. Door het adres vast te leggen in Verzending is altijd te achterhalen naar/van welk adres
    een Informatieobject verstuurd/ontvangen is.

    Het attribuut richting uit de relatieklasse ZaaktypeInformatieobjecttype en de attributen ontvangstdatum en verzenddatum
    uit Einkelvoudiginformatieobject zijn hiermee overbodig en deprecated geworden.
*/
[ApiController]
[Authorize]
[ZgwApiVersion(Api.LatestVersion_1_5)]
[Consumes("application/json")]
[Produces("application/json")]
public class VerzendingenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly IObjectExpander<InformatieObjectContext> _expander;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public VerzendingenController(
        ILogger<VerzendingenController> logger,
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
        _expander = expanderFactory.Create<InformatieObjectContext>("informatieobject");
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/verzendingen

    /// <summary>
    /// Alle VERZENDINGen opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Verzendingen.GetAll, Name = Operations.Verzendingen.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<VerzendingResponseExpandedDto>))]
    [Expand]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] GetAllVerzendingenQueryParameters queryParameters,
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.VerzendingenPageSize));
        var filter = _mapper.Map<GetAllVerzendingenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllVerzendingenQuery { GetAllVerzendingenFilter = filter, Pagination = pagination },
            cancellationToken
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var verzendingenResponse = _mapper.Map<List<VerzendingResponseDto>>(result.Result.PageResult);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var verzendingenWithOptionalExpand = verzendingenResponse
            .Select(v =>
                _expander.ResolveAsync(expandLookup, new InformatieObjectContext { InformatieObject = v.InformatieObject, ObjectDto = v }).Result
            )
            .ToList();

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            verzendingenWithOptionalExpand,
            result.Result.Count
        );

        await _mediator.Send(
            new LogAuditTrailGetObjectListCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                Page = pagination.Page,
                Count = paginationResponse.Results.Count(),
                TotalCount = paginationResponse.Count,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "verzending" },
            },
            cancellationToken
        );

        return Ok(paginationResponse);
    }

    //
    // HTTP GET http://documenten.user.local:5007/api/v1/verzendingen/b24ee37c-00db-4108-b831-e3b420b35a09

    /// <summary>
    /// Een specifieke VERZENDING opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.Verzendingen.Get, Name = Operations.Verzendingen.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(VerzendingResponseExpandedDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetVerzendingQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(GetAsync), id, queryParameters);

        var result = await _mediator.Send(new GetVerzendingQuery { Id = id }, cancellationToken);

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var verzending = _mapper.Map<VerzendingResponseDto>(result.Result);

        var expandLookup = ExpandLookup(queryParameters.Expand);

        var verzendingWithOptionalExpand = await _expander.ResolveAsync(
            expandLookup,
            new InformatieObjectContext { InformatieObject = verzending.InformatieObject, ObjectDto = verzending }
        );

        await _mediator.Send(
            new LogAuditTrailGetObjectCommand
            {
                RetrieveCatagory = RetrieveCatagory.Minimal,
                BaseEntity = result.Result,
                SubEntity = result.Result,
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "verzending" },
            },
            cancellationToken
        );

        return Ok(verzendingWithOptionalExpand);
    }

    /// <summary>
    /// De headers voor een specifiek(e) VERZENDING opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.Verzendingen.Get, Name = Operations.Verzendingen.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    public Task<IActionResult> HeadAsync(Guid id, [FromQuery] GetVerzendingQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        return GetAsync(id, queryParameters, cancellationToken);
    }

    //
    // HTTP POST http://documenten.user.local:5007/api/v1/verzendingen

    /// <summary>
    /// Maak een VERZENDING aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.Verzendingen.Create, Name = Operations.Verzendingen.Create)]
    [Scope(AuthorizationScopes.Documenten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(VerzendingResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] VerzendingRequestDto verzendingRequest, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), verzendingRequest);

        var verzending = _mapper.Map<Verzending>(verzendingRequest);

        var result = await _mediator.Send(
            new CreateVerzendingCommand { Verzending = verzending, InformatieObjectUrl = verzendingRequest.InformatieObject },
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

        var verzendingResponse = _mapper.Map<VerzendingResponseDto>(result.Result);

        return Created(verzendingResponse.Url, verzendingResponse);
    }

    //
    // HTTP PUT http://documenten.user.local:5007/api/v1/verzendingen/59bad509-840b-4cd0-82dc-cbda74a75c2b

    /// <summary>
    /// Werk een VERZENDING in zijn geheel bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.Verzendingen.Update, Name = Operations.Verzendingen.Update)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(VerzendingResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] VerzendingRequestDto verzendingRequest, Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(UpdateAsync), verzendingRequest);

        var verzending = _mapper.Map<Verzending>(verzendingRequest);

        var result = await _mediator.Send(
            new UpdateVerzendingCommand
            {
                Id = id,
                InformatieObjectUrl = verzendingRequest.InformatieObject,
                Verzending = verzending,
                IsPartialUpdate = false,
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

        var verzendingResponse = _mapper.Map<VerzendingResponseDto>(result.Result);

        return Ok(verzendingResponse);
    }

    //
    // HTTP PATCH http://documenten.user.local:5007/api/v1/verzendingen/59bad509-840b-4cd0-82dc-cbda74a75c2b

    /// <summary>
    /// Werk een VERZENDING relatie deels bij.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.Verzendingen.Update, Name = Operations.Verzendingen.PartialUpdate)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(VerzendingResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialVerzendingRequest, Guid id, CancellationToken cancellationToken)
    {
        // We do log only the request not the partial update request (because can be large)
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetVerzendingQuery { Id = id }, cancellationToken);

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (resultGet.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        VerzendingRequestDto mergedVerzendingRequest = _requestMerger.MergePartialUpdateToObjectRequest<VerzendingRequestDto, Verzending>(
            resultGet.Result,
            partialVerzendingRequest
        );

        if (!_validatorService.IsValid(mergedVerzendingRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        Verzending mergedVerzending = _mapper.Map<Verzending>(mergedVerzendingRequest);

        var result = await _mediator.Send(
            new UpdateVerzendingCommand
            {
                Id = id,
                Verzending = mergedVerzending,
                InformatieObjectUrl = mergedVerzendingRequest.InformatieObject,
                IsPartialUpdate = true,
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

        var verzendingResponse = _mapper.Map<VerzendingResponseDto>(result.Result);

        return Ok(verzendingResponse);
    }

    //
    // HTTP DELETE https://documenten-api.vng.cloud/api/v1/verzendingen/b24ee37c-00db-4108-b831-e3b420b35a09

    /// <summary>
    /// Verwijder een VERZENDING.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.Verzendingen.Delete, Name = Operations.Verzendingen.Delete)]
    [Scope(AuthorizationScopes.Documenten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteVerzendingCommand { Id = id }, cancellationToken);

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
