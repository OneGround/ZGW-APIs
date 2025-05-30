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
using Roxit.ZGW.Catalogi.Contracts.v1._3.Queries;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Requests;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Authorization;
using Roxit.ZGW.Catalogi.Web.Configuration;
using Roxit.ZGW.Catalogi.Web.Contracts.v1._3;
using Roxit.ZGW.Catalogi.Web.Handlers.v1._3;
using Roxit.ZGW.Catalogi.Web.Models.v1._3;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Filters;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Catalogi.Web.Controllers.v1._3;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_3)]
public class ZaakObjectTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZaakObjectTypeController(
        ILogger<ZaakObjectTypeController> logger,
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
    /// Alle ZAAKOBJECTTYPEn opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjectTypen.GetAll, Name = Operations.ZaakObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakObjectTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakObjectTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakObjectTypenPageSize));
        var filter = _mapper.Map<GetAllZaakObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakObjectTypenQuery { GetAllZaakObjectTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var zaakobjecttypenResponse = _mapper.Map<List<ZaakObjectTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, zaakobjecttypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke ZAAKOBJECTTYPE opvragen.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakObjectTypen.Get, Name = Operations.ZaakObjectTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectTypeResponseDto))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakObjectTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var zaakobjecttypeResponse = _mapper.Map<ZaakObjectTypeResponseDto>(result.Result);

        return Ok(zaakobjecttypeResponse);
    }

    /// <summary>
    /// De headers voor een specifiek(e) ZAAKOBJECTTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.ZaakObjectTypen.Get, Name = Operations.ZaakObjectTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
    }

    /// <summary>
    /// Maak een ZAAKOBJECTTYPE aan.
    /// </summary>
    /// <remarks>
    /// Maak een ZAAKOBJECTTYPE aan. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakObjectTypen.Create, Name = Operations.ZaakObjectTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakObjectTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakObjectTypeRequestDto zaakObjectTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakObjectTypeRequest);

        ZaakObjectType zaakObjectType = _mapper.Map<ZaakObjectType>(zaakObjectTypeRequest);

        var result = await _mediator.Send(
            new CreateZaakObjectTypeCommand { ZaakObjectType = zaakObjectType, ZaakType = zaakObjectTypeRequest.ZaakType }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var zaakObjectTypeResponse = _mapper.Map<ZaakObjectTypeResponseDto>(result.Result);

        return Created(zaakObjectTypeResponse.Url, zaakObjectTypeResponse);
    }

    /// <summary>
    /// Werk een ZAAKOBJECTTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een ZAAKOBJECTTYPE in zijn geheel bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakObjectTypen.Update, Name = Operations.ZaakObjectTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakObjectTypeRequestDto zaakObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), zaakObjectTypeRequest, id);

        ZaakObjectType zaakObjectType = _mapper.Map<ZaakObjectType>(zaakObjectTypeRequest);

        var result = await _mediator.Send(
            new UpdateZaakObjectTypeCommand
            {
                ZaakObjectType = zaakObjectType,
                Id = id,
                ZaakType = zaakObjectTypeRequest.ZaakType,
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

        var zaakObjectTypeResponse = _mapper.Map<ZaakObjectTypeResponseDto>(result.Result);

        return Ok(zaakObjectTypeResponse);
    }

    /// <summary>
    /// Werk een ZAAKOBJECTTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een ZAAKOBJECTTYPE deels bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakObjectTypen.Update, Name = Operations.ZaakObjectTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakObjectTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakObjectTypeQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ZaakObjectTypeRequestDto mergedZaakObjectTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            ZaakObjectTypeRequestDto,
            ZaakObjectType
        >(resultGet.Result, partialZaakObjectTypeRequest);

        if (!_validatorService.IsValid(mergedZaakObjectTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakObjectType mergedZaakObjectType = _mapper.Map<ZaakObjectType>(mergedZaakObjectTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateZaakObjectTypeCommand
            {
                ZaakObjectType = mergedZaakObjectType,
                Id = id,
                ZaakType = mergedZaakObjectTypeRequest.ZaakType,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var zaakObjectTypeResponse = _mapper.Map<ZaakObjectTypeResponseDto>(resultUpd.Result);

        return Ok(zaakObjectTypeResponse);
    }

    /// <summary>
    /// Verwijder een ZAAKOBJECTTYPE.
    /// </summary>
    /// <remarks>
    /// Verwijder een ZAAKOBJECTTYPE. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakObjectTypen.Delete, Name = Operations.ZaakObjectTypen.Delete)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakObjectTypeCommand { Id = id });

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
}
