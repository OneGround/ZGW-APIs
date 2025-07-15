using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Filters;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Catalogi.Web.Controllers.v1._3;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_3)]
public class ZaakTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakTypeController(
        ILogger<ZaakTypeController> logger,
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
    /// Alle ZAAKTYPEn opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypen.GetAll, Name = Operations.ZaakTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Catalogi.Contracts.v1.Queries.GetAllZaakTypenQueryParameters queryParameters,
        int page = 1
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakTypenPageSize));
        var filter = _mapper.Map<Models.v1.GetAllZaakTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakTypenQuery { GetAllZaakTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaaktypenResponse = _mapper.Map<List<ZaakTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaaktypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke ZAAKTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypen.Get, Name = Operations.ZaakTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeResponseDto))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var zaaktypeResponse = _mapper.Map<ZaakTypeResponseDto>(result.Result);

        return Ok(zaaktypeResponse);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ZAAKTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakTypen.Get, Name = Operations.ZaakTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
    }

    /// <summary>
    /// Maak een ZAAKTYPE aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakTypen.Create, Name = Operations.ZaakTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakTypeRequestDto zaakTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakTypeRequest);

        ZaakType zaakType = _mapper.Map<ZaakType>(zaakTypeRequest);

        var result = await _mediator.Send(
            new CreateZaakTypeCommand
            {
                ZaakType = zaakType,
                Catalogus = zaakTypeRequest.Catalogus,
                DeelZaakTypen = zaakTypeRequest.DeelZaakTypen,
                BesluitTypen = zaakTypeRequest.BesluitTypen,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<ZaakTypeResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Werk een ZAAKTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een ZAAKTYPE in zijn geheel bij. Dit kan alleen als het een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakTypen.Update, Name = Operations.ZaakTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakTypeRequestDto zaakTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakTypeRequest, id);

        ZaakType zaaktype = _mapper.Map<ZaakType>(zaakTypeRequest);

        var result = await _mediator.Send(
            new UpdateZaakTypeCommand
            {
                Id = id,
                ZaakType = zaaktype,
                Catalogus = zaakTypeRequest.Catalogus,
                DeelZaakTypen = zaakTypeRequest.DeelZaakTypen,
                BesluitTypen = zaakTypeRequest.BesluitTypen,
                IsPartialUpdate = false,
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

        var zaakTypeResponse = _mapper.Map<ZaakTypeResponseDto>(result.Result);

        return Ok(zaakTypeResponse);
    }

    /// <summary>
    /// Werk een ZAAKTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een ZAAKTYPE deels bij. Dit kan alleen als het een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakTypen.Update, Name = Operations.ZaakTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakTypeQuery { Id = id, IncludeSoftRelations = false });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ZaakTypeRequestDto mergedZaakTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<ZaakTypeRequestDto, ZaakType>(
            resultGet.Result,
            partialZaakTypeRequest
        );

        if (!_validatorService.IsValid(mergedZaakTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakType mergedZaakType = _mapper.Map<ZaakType>(mergedZaakTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakTypeCommand
            {
                ZaakType = mergedZaakType,
                Id = id,
                Catalogus = mergedZaakTypeRequest.Catalogus,
                BesluitTypen = mergedZaakTypeRequest.BesluitTypen,
                DeelZaakTypen = mergedZaakTypeRequest.DeelZaakTypen,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var response = _mapper.Map<ZaakTypeResponseDto>(resultUpd.Result);

        return Ok(response);
    }

    /// <summary>
    /// Verwijder een ZAAKTYPE.
    /// </summary>
    /// <remarks>
    /// Verwijder een ZAAKTYPE. Dit kan alleen als het een concept betreft.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakTypen.Delete, Name = Operations.ZaakTypen.Delete)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakTypeCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Opvragen en bewerken van ZAAKTYPEn nodig voor ZAKEN in de Zaken API.
    /// </summary>
    /// <remarks>
    /// Een ZAAKTYPE beschrijft het geheel van karakteristieke eigenschappen van zaken van eenzelfde soort.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakTypen.Publish, Name = Operations.ZaakTypen.Publish)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> PublishAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PublishAsync), id);

        var result = await _mediator.Send(new PublishZaakTypeCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<ZaakTypeResponseDto>(result.Result);

        return Ok(response);
    }
}
