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
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
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
public class BesluitTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public BesluitTypeController(
        ILogger<BesluitTypeController> logger,
        IMediator mediator,
        IMapper mapper,
        IRequestMerger requestMerger,
        IConfiguration configuration,
        IErrorResponseBuilder errorResponseBuilder,
        IPaginationHelper paginationHelper,
        IValidatorService validatorService
    )
        : base(logger, mediator, mapper, requestMerger, errorResponseBuilder)
    {
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    /// <summary>
    /// Een specifieke BESLUITTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.BesluitTypen.Get, Name = Operations.BesluitTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitTypeResponseDto))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetBesluitTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<BesluitTypeResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// De headers voor een specifiek(e) BESLUITTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.BesluitTypen.Get, Name = Operations.BesluitTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
    }

    /// <summary>
    /// Maak een BESLUITTYPE aan.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.BesluitTypen.Create, Name = Operations.BesluitTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(BesluitTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] BesluitTypeRequestDto besluitTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), besluitTypeRequest);

        var besluitType = _mapper.Map<BesluitType>(besluitTypeRequest);

        var result = await _mediator.Send(
            new CreateBesluitTypeCommand
            {
                BesluitType = besluitType,
                Catalogus = besluitTypeRequest.Catalogus,
                ZaakTypen = besluitTypeRequest.ZaakTypen,
                InformatieObjectTypen = besluitTypeRequest.InformatieObjectTypen,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<BesluitTypeResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Alle BESLUITTYPEn opvragen.
    /// </summary>
    /// <remarks>
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.BesluitTypen.GetAll, Name = Operations.BesluitTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<BesluitTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllBesluitTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.BesluitTypenPageSize));
        var filter = _mapper.Map<GetAllBesluitTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllBesluitTypenQuery { GetAllBesluitTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var statustypenResponse = _mapper.Map<List<BesluitTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, statustypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Werk een BESLUITTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een BESLUITTYPE in zijn geheel bij. Dit kan alleen als het een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.BesluitTypen.Update, Name = Operations.BesluitTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] BesluitTypeRequestDto besluitTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), besluitTypeRequest, id);

        BesluitType besluitType = _mapper.Map<BesluitType>(besluitTypeRequest);

        var result = await _mediator.Send(
            new UpdateBesluitTypeCommand
            {
                BesluitType = besluitType,
                Id = id,
                Catalogus = besluitTypeRequest.Catalogus,
                ZaakTypen = besluitTypeRequest.ZaakTypen,
                InformatieObjectTypen = besluitTypeRequest.InformatieObjectTypen,
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

        var resultTypeResponse = _mapper.Map<BesluitTypeResponseDto>(result.Result);

        return Ok(resultTypeResponse);
    }

    /// <summary>
    /// Werk een BESLUITTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een BESLUITTYPE deels bij. Dit kan alleen als het een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.BesluitTypen.Update, Name = Operations.BesluitTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialBesluitTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetBesluitTypeQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        BesluitTypeRequestDto mergedBesluitTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<BesluitTypeRequestDto, BesluitType>(
            resultGet.Result,
            partialBesluitTypeRequest
        );

        if (!_validatorService.IsValid(mergedBesluitTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        BesluitType mergedBesluitType = _mapper.Map<BesluitType>(mergedBesluitTypeRequest);

        var besluitTypeUpdate = await _mediator.Send(
            new UpdateBesluitTypeCommand
            {
                BesluitType = mergedBesluitType,
                Id = id,
                Catalogus = mergedBesluitTypeRequest.Catalogus,
                ZaakTypen = mergedBesluitTypeRequest.ZaakTypen,
                InformatieObjectTypen = mergedBesluitTypeRequest.InformatieObjectTypen,
                IsPartialUpdate = true,
            }
        );

        if (besluitTypeUpdate.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(besluitTypeUpdate.Errors);
        }

        var response = _mapper.Map<BesluitTypeResponseDto>(besluitTypeUpdate.Result);

        return Ok(response);
    }

    /// <summary>
    /// Verwijder een BESLUITTYPE.
    /// </summary>
    /// <remarks>
    /// Verwijder een BESLUITTYPE. Dit kan alleen als het een concept betreft.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [HttpDelete(ApiRoutes.BesluitTypen.Delete, Name = Operations.BesluitTypen.Delete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteBesluitTypeCommand { Id = id });

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
    /// Opvragen en bewerken van BESLUITTYPEn nodig voor BESLUITEN in de Besluiten API.
    /// </summary>
    /// <remarks>
    /// Alle BESLUITTYPEn van de besluiten die het resultaat kunnen zijn van het zaakgericht werken van de behandelende organisatie(s).
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.BesluitTypen.Publish, Name = Operations.BesluitTypen.Publish)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(BesluitTypeResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> PublishAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PublishAsync), id);

        var result = await _mediator.Send(new PublishBesluitTypeCommand { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<BesluitTypeResponseDto>(result.Result);

        return Ok(response);
    }
}
