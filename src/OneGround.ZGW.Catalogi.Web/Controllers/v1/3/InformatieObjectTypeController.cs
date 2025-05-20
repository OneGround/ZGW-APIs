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
public class InformatieObjectTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public InformatieObjectTypeController(
        ILogger<InformatieObjectTypeController> logger,
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
    /// Get all INFORMATIEOBJECTTYPEN.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.InformatieObjectTypen.GetAll, Name = Operations.InformatieObjectTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<InformatieObjectTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Catalogi.Contracts.v1._2.Queries.GetAllInformatieObjectTypenQueryParameters queryParameters,
        int page = 1
    )
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.InformatieObjectTypenPageSize));
        var filter = _mapper.Map<Models.v1.GetAllInformatieObjectTypenFilter>(queryParameters);

        var result = await _mediator.Send(
            new GetAllInformatieObjectTypenQuery() { GetAllInformatieObjectTypenFilter = filter, Pagination = pagination }
        );

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var informatieObjectTypenResponse = _mapper.Map<List<InformatieObjectTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(
            queryParameters,
            pagination,
            informatieObjectTypenResponse,
            result.Result.Count
        );

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Get INFORMATIEOBJECTTYPE by id.
    /// </summary>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.InformatieObjectTypen.Get, Name = Operations.InformatieObjectTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(InformatieObjectTypeResponseDto))]
    [ETagFilter]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetInformatieObjectTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var informatieObjectTypeResponseDto = _mapper.Map<InformatieObjectTypeResponseDto>(result.Result);

        return Ok(informatieObjectTypeResponseDto);
    }

    /// <summary>
    /// De headers voor een specifiek(e) INFORMATIEOBJECTTYPE opvragen
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="304">Not Modified</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpHead(ApiRoutes.InformatieObjectTypen.Get, Name = Operations.InformatieObjectTypen.ReadHead)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [ETagFilter]
    public Task<IActionResult> HeadAsync(Guid id)
    {
        return GetAsync(id);
    }

    /// <summary>
    /// Create INFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.InformatieObjectTypen.Create, Name = Operations.InformatieObjectTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(InformatieObjectTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] InformatieObjectTypeRequestDto informatieObjectTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), informatieObjectTypeRequest);

        InformatieObjectType informatieObjectType = _mapper.Map<InformatieObjectType>(informatieObjectTypeRequest);

        var result = await _mediator.Send(
            new CreateInformatieObjectTypeCommand()
            {
                InformatieObjectType = informatieObjectType,
                CatalogusUrl = informatieObjectTypeRequest.Catalogus,
            }
        );

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<InformatieObjectTypeResponseDto>(result.Result);

        return Created(response.Url, response);
    }

    /// <summary>
    /// Update INFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.InformatieObjectTypen.Update, Name = Operations.InformatieObjectTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(InformatieObjectTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] InformatieObjectTypeRequestDto informatieObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), informatieObjectTypeRequest, id);

        InformatieObjectType informatieObjectType = _mapper.Map<InformatieObjectType>(informatieObjectTypeRequest);

        var result = await _mediator.Send(
            new UpdateInformatieObjectTypeCommand
            {
                Catalogus = informatieObjectTypeRequest.Catalogus,
                Id = id,
                InformatieObjectType = informatieObjectType,
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

        var informatieObjectTypeResponseDto = _mapper.Map<InformatieObjectTypeResponseDto>(result.Result);

        return Ok(informatieObjectTypeResponseDto);
    }

    /// <summary>
    /// Partially update INFORMATIEOBJECTTYPE.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.InformatieObjectTypen.Update, Name = Operations.InformatieObjectTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedUpdate)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(InformatieObjectTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialInformatieObjectTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetInformatieObjectTypeQuery() { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        InformatieObjectTypeRequestDto mergedInformatieObjectTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<
            InformatieObjectTypeRequestDto,
            InformatieObjectType
        >(resultGet.Result, partialInformatieObjectTypeRequest);

        if (!_validatorService.IsValid(mergedInformatieObjectTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        InformatieObjectType mergedInformatieObjectType = _mapper.Map<InformatieObjectType>(mergedInformatieObjectTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateInformatieObjectTypeCommand()
            {
                InformatieObjectType = mergedInformatieObjectType,
                Id = id,
                Catalogus = mergedInformatieObjectTypeRequest.Catalogus,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var informatieObjectTypeResponseDto = _mapper.Map<InformatieObjectTypeResponseDto>(resultUpd.Result);

        return Ok(informatieObjectTypeResponseDto);
    }

    /// <summary>
    /// Delete INFORMATIEOBJECTTYPE by id.
    /// </summary>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.InformatieObjectTypen.Delete, Name = Operations.InformatieObjectTypen.Delete)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteInformatieObjectTypeCommand { Id = id });

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
    /// Opvragen en bewerken van INFORMATIEOBJECTTYPEn nodig voor INFORMATIEOBJECTen in de Documenten API.
    /// </summary>
    /// <remarks>
    /// Een INFORMATIEOBJECTTYPE beschijft de karakteristieken van een document of ander object dat informatie bevat.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.InformatieObjectTypen.Publish, Name = Operations.InformatieObjectTypen.Publish)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(InformatieObjectTypeResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> PublishAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PublishAsync), id);

        var result = await _mediator.Send(new PublishInformatieObjectTypeCommand() { Id = id });

        if (result.Status == CommandStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var response = _mapper.Map<InformatieObjectTypeResponseDto>(result.Result);

        return Ok(response);
    }
}
