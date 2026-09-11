using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
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
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Documenten.Web.Controllers.v1._7;

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
[ZgwApiVersion(Api.LatestVersion_1_7)]
[Consumes("application/json")]
[Produces("application/json")]
public class VerzendingenController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly ExpandValidator<VerzendingResponseDto> _expandValidator;
    private readonly ExpandEngine<VerzendingResponseDto> _expandEngine;

    public VerzendingenController(
        ILogger<VerzendingenController> logger,
        IMediator mediator,
        MapsterMapper.IMapper mapper,
        IConfiguration configuration,
        IPaginationHelper paginationHelper,
        IErrorResponseBuilder errorResponseBuilder,
        ExpandValidator<VerzendingResponseDto> expandValidator,
        ExpandEngine<VerzendingResponseDto> expandEngine
    )
        : base(logger, mediator, mapper, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _expandValidator = expandValidator;
        _expandEngine = expandEngine;

        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
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
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.Verzendingen.GetAll, Name = Contracts.v1._5.Operations.Verzendingen.List)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<VerzendingResponseDto>))]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetAllVerzendingenQueryParameters>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] GetAllVerzendingenQueryParameters queryParameters,
        int page = 1,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

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

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.VerzendingenPageSize));
        var filter = _mapper.Map<Models.v1._5.GetAllVerzendingenFilter>(queryParameters);

        var result = await _mediator.Send(
            new Handlers.v1._5.GetAllVerzendingenQuery { GetAllVerzendingenFilter = filter, Pagination = pagination },
            cancellationToken
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var verzendingenResponse = _mapper.Map<List<VerzendingResponseDto>>(result.Result.PageResult);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveListAsync(verzendingenResponse, expandPaths);
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

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, verzendingenResponse, result.Result.Count);

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
    /// <response code="502">Bad Gateway</response>
    [HttpGet(Contracts.v1._5.ApiRoutes.Verzendingen.Get, Name = Contracts.v1._5.Operations.Verzendingen.Read)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(VerzendingResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ETagFilter]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetVerzendingQueryParameters>))]
    public async Task<IActionResult> GetAsync(Guid id, [FromQuery] GetVerzendingQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}, {@FromQuery}", nameof(GetAsync), id, queryParameters);

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

        var result = await _mediator.Send(new Handlers.v1._5.GetVerzendingQuery { Id = id }, cancellationToken);

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == QueryStatus.Forbidden)
        {
            return _errorResponseBuilder.Forbidden();
        }

        var verzending = _mapper.Map<VerzendingResponseDto>(result.Result);

        // Handle optional expands on the returned DTO. This is done after the mapping to the DTO, because the expand resolvers are registered for the DTO type, not for the entity type.
        if (expandPaths is { Count: > 0 })
        {
            try
            {
                await _expandEngine.ResolveAsync(verzending, expandPaths);
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
                AuditTrailOptions = new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "verzending" },
                LegacyAuditTrail = result.Result.InformatieObject.LegacyAuditTrail,
            },
            cancellationToken
        );

        return Ok(verzending);
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
    /// <response code="502">Bad Gateway</response>
    [HttpHead(Contracts.v1._5.ApiRoutes.Verzendingen.Get, Name = Contracts.v1._5.Operations.Verzendingen.ReadHead)]
    [Scope(AuthorizationScopes.Documenten.Read)]
    [ETagFilter]
    [Expand]
    [ServiceFilter(typeof(ValidateQueryParametersFilter<GetVerzendingQueryParameters>))]
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
    [HttpPost(Contracts.v1._5.ApiRoutes.Verzendingen.Create, Name = Contracts.v1._5.Operations.Verzendingen.Create)]
    [Scope(AuthorizationScopes.Documenten.Create)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(VerzendingResponseDto))]
    public async Task<IActionResult> AddAsync(
        [FromBody] Documenten.Contracts.v1._5.Requests.VerzendingRequestDto verzendingRequest,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), verzendingRequest);

        var verzending = _mapper.Map<Verzending>(verzendingRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.CreateVerzendingCommand
            {
                Verzending = verzending,
                InformatieObjectUrl = verzendingRequest.InformatieObject,
                Version = IsApiVersionRequested(new ApiVersion(1, 7)) ? 1.7M : 1.5M,
            },
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
    /// <response code="409">Verzending was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(Contracts.v1._5.ApiRoutes.Verzendingen.Update, Name = Contracts.v1._5.Operations.Verzendingen.Update)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(VerzendingResponseDto))]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] Documenten.Contracts.v1._5.Requests.VerzendingRequestDto verzendingRequest,
        Guid id,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(UpdateAsync), verzendingRequest);

        var verzending = _mapper.Map<Verzending>(verzendingRequest);

        var result = await _mediator.Send(
            new Handlers.v1._5.UpdateVerzendingCommand
            {
                Id = id,
                InformatieObjectUrl = verzendingRequest.InformatieObject,
                Version = IsApiVersionRequested(new ApiVersion(1, 7)) ? 1.7M : 1.5M,
                Verzending = verzending, // Note: Indicates that the versie should be fully replaced in the command handler
                PartialObject = null,
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
    /// <response code="409">Verzending was modified by another user</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(Contracts.v1._5.ApiRoutes.Verzendingen.Update, Name = Contracts.v1._5.Operations.Verzendingen.PartialUpdate)]
    [Scope(AuthorizationScopes.Documenten.Update)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(VerzendingResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialVerzendingRequest, Guid id, CancellationToken cancellationToken)
    {
        // We do log only the request not the partial update request (because can be large)
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var result = await _mediator.Send(
            new Handlers.v1._5.UpdateVerzendingCommand
            {
                Id = id,
                InformatieObjectUrl = GetValueFromPartial<string>(partialVerzendingRequest, "informatieObject"),
                Version = IsApiVersionRequested(new ApiVersion(1, 7)) ? 1.7M : 1.5M,
                Verzending = null,
                PartialObject = partialVerzendingRequest, // Note: Indicates that the versie should be merged in the command handler
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
    [HttpDelete(Contracts.v1._5.ApiRoutes.Verzendingen.Delete, Name = Contracts.v1._5.Operations.Verzendingen.Delete)]
    [Scope(AuthorizationScopes.Documenten.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new Handlers.v1._5.DeleteVerzendingCommand { Id = id }, cancellationToken);

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
