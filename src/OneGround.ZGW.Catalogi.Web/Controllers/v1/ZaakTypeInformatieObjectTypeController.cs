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
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Authorization;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Catalogi.Web.Contracts.v1;
using OneGround.ZGW.Catalogi.Web.Handlers.v1;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace OneGround.ZGW.Catalogi.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class ZaakTypeInformatieObjectTypeController : ZGWControllerBase
{
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;

    public ZaakTypeInformatieObjectTypeController(
        ILogger<ZaakTypeInformatieObjectTypeController> logger,
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
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _paginationHelper = paginationHelper;
        _validatorService = validatorService;
    }

    /// <summary>
    /// Get all ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypeInformatieObjectTypen.GetAll, Name = Operations.ZaakTypeInformatieObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllZaakTypeInformatieObjectTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.ZaakTypeInformatieObjectTypenPageSize));
        var filter = _mapper.Map<GetAllZaakTypeInformatieObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllZaakTypeInformatieObjectTypenQuery { GetAllZaakTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var getAllZaakTypeInformatieObjectTypenQueryResponse = _mapper.Map<List<ZaakTypeInformatieObjectTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            getAllZaakTypeInformatieObjectTypenQueryResponse,
            result.Result.Count
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Get ZAAKTYPEINFORMATIEOBJECTTYPE by id.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.ZaakTypeInformatieObjectTypen.Get, Name = Operations.ZaakTypeInformatieObjectTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetZaakTypeInformatieObjectTypeQuery() { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var response = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(result.Result);

        return Ok(response);
    }

    /// <summary>
    /// Create ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <param name="zaakTypeInformatieObjectTypeRequest"></param>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.ZaakTypeInformatieObjectTypen.Create, Name = Operations.ZaakTypeInformatieObjectTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] ZaakTypeInformatieObjectTypeRequestDto zaakTypeInformatieObjectTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), zaakTypeInformatieObjectTypeRequest);

        var zaakTypeInformatieObjectType = _mapper.Map<ZaakTypeInformatieObjectType>(zaakTypeInformatieObjectTypeRequest);

        var result = await _mediator.Send(
            new CreateZaakTypeInformatieObjectTypenCommand
            {
                ZaakTypeInformatieObjectType = zaakTypeInformatieObjectType,
                ZaakType = zaakTypeInformatieObjectTypeRequest.ZaakType,
                StatusType = zaakTypeInformatieObjectTypeRequest.StatusType,
                InformatieObjectType = zaakTypeInformatieObjectTypeRequest.InformatieObjectType,
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

        var response = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(
            result.Result,
            opt => opt.AfterMap((_, dest) => dest.InformatieObjectType = zaakTypeInformatieObjectTypeRequest.InformatieObjectType)
        );

        return Created(response.Url, response);
    }

    /// <summary>
    /// Update ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.ZaakTypeInformatieObjectTypen.Update, Name = Operations.ZaakTypeInformatieObjectTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] ZaakTypeInformatieObjectTypeRequestDto request, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(GetAllAsync), request, id);

        var zaakTypeInformatieObjectType = _mapper.Map<ZaakTypeInformatieObjectType>(request);

        var result = await _mediator.Send(
            new UpdateZaakTypeInformatieObjectTypeCommand()
            {
                ZaakTypeInformatieObjectType = zaakTypeInformatieObjectType,
                ZaakType = request.ZaakType,
                InformatieObjectType = request.InformatieObjectType,
                StatusType = request.StatusType,
                Id = id,
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

        var response = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(
            result.Result,
            opt => opt.AfterMap((_, dest) => dest.InformatieObjectType = request.InformatieObjectType)
        );

        return Ok(response);
    }

    /// <summary>
    /// Partially update ZAAKTYPEINFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.ZaakTypeInformatieObjectTypen.Update, Name = Operations.ZaakTypeInformatieObjectTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(ZaakTypeInformatieObjectTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialZaakTypeInformatieObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetZaakTypeInformatieObjectTypeQuery() { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        ZaakTypeInformatieObjectTypeRequestDto mergedZaakTypeInformatieObjectTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            ZaakTypeInformatieObjectTypeRequestDto,
            ZaakTypeInformatieObjectType
        >(resultGet.Result, partialZaakTypeInformatieObjectTypeRequest);

        if (!_validatorService.IsValid(mergedZaakTypeInformatieObjectTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        ZaakTypeInformatieObjectType mergedZaakTypeInformatieObjectType = _mapper.Map<ZaakTypeInformatieObjectType>(
            mergedZaakTypeInformatieObjectTypeRequest
        );

        var resultUpd = await _mediator.Send(
            new UpdateZaakTypeInformatieObjectTypeCommand()
            {
                Id = id,
                ZaakTypeInformatieObjectType = mergedZaakTypeInformatieObjectType,
                StatusType = mergedZaakTypeInformatieObjectTypeRequest.StatusType,
                ZaakType = mergedZaakTypeInformatieObjectTypeRequest.ZaakType,
                InformatieObjectType = mergedZaakTypeInformatieObjectTypeRequest.InformatieObjectType,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var zaakTypeInformatieObjectTypeResponseDto = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(
            resultUpd.Result,
            opt => opt.AfterMap((_, dest) => dest.InformatieObjectType = mergedZaakTypeInformatieObjectTypeRequest.InformatieObjectType)
        );

        return Ok(zaakTypeInformatieObjectTypeResponseDto);
    }

    /// <summary>
    /// Delete ZAAKTYPEINFORMATIEOBJECTTYPE
    /// </summary>
    /// <param name="id"></param>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.ZaakTypeInformatieObjectTypen.Delete, Name = Operations.ZaakTypeInformatieObjectTypen.Delete)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteZaakTypeInformatieObjectTypeCommand() { Id = id });

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
