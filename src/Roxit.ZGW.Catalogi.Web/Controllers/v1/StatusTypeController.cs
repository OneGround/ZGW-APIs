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
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Authorization;
using Roxit.ZGW.Catalogi.Web.Configuration;
using Roxit.ZGW.Catalogi.Web.Contracts.v1;
using Roxit.ZGW.Catalogi.Web.Handlers.v1;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Controllers;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Versioning;
using Swashbuckle.AspNetCore.Annotations;

namespace Roxit.ZGW.Catalogi.Web.Controllers.v1;

[ApiController]
[Authorize]
[Consumes("application/json")]
[Produces("application/json")]
[ZgwApiVersion(Api.LatestVersion_1_0)]
public class StatusTypeController : ZGWControllerBase
{
    private readonly IPaginationHelper _paginationHelper;
    private readonly IValidatorService _validatorService;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public StatusTypeController(
        ILogger<StatusTypeController> logger,
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
    /// Alle STATUSTYPEn opvragen.
    /// Deze lijst kan gefilterd wordt met query-string parameters.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.StatusTypen.GetAll, Name = Operations.StatusTypen.List)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(PagedResponse<StatusTypeResponseDto>))]
    public async Task<IActionResult> GetAllAsync([FromQuery] GetAllStatusTypenQueryParameters queryParameters, int page = 1)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromQuery}, {Page}", nameof(GetAllAsync), queryParameters, page);

        var pagination = _mapper.Map<PaginationFilter>(new PaginationQuery(page, _applicationConfiguration.StatusTypenPageSize));
        var filter = _mapper.Map<GetAllStatusTypenFilter>(queryParameters);

        var result = await _mediator.Send(new GetAllStatusTypenQuery { GetAllStatusTypenFilter = filter, Pagination = pagination });

        if (!_paginationHelper.ValidatePaginatedResponse(pagination, result.Result.Count))
        {
            return _errorResponseBuilder.PageNotFound();
        }

        var statustypenResponse = _mapper.Map<List<StatusTypeResponseDto>>(result.Result.PageResult);

        var paginationResponse = _paginationHelper.CreatePaginatedResponse(queryParameters, pagination, statustypenResponse, result.Result.Count);

        return Ok(paginationResponse);
    }

    /// <summary>
    /// Een specifieke STATUSTYPE opvragen.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet(ApiRoutes.StatusTypen.Get, Name = Operations.StatusTypen.Read)]
    [Scope(AuthorizationScopes.Catalogi.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(StatusTypeResponseDto))]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(GetAsync), id);

        var result = await _mediator.Send(new GetStatusTypeQuery { Id = id });

        if (result.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        var statustypeResponse = _mapper.Map<StatusTypeResponseDto>(result.Result);

        return Ok(statustypeResponse);
    }

    /// <summary>
    /// Maak een STATUSTYPE aan.
    /// </summary>
    /// <remarks>
    /// Maak een STATUSTYPE aan. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost(ApiRoutes.StatusTypen.Create, Name = Operations.StatusTypen.Create)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status201Created, Type = typeof(StatusTypeResponseDto))]
    public async Task<IActionResult> AddAsync([FromBody] StatusTypeRequestDto statusTypeRequest)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}", nameof(AddAsync), statusTypeRequest);

        StatusType statusType = _mapper.Map<StatusType>(statusTypeRequest);

        var result = await _mediator.Send(new CreateStatusTypeCommand { StatusType = statusType, ZaakType = statusTypeRequest.ZaakType });

        if (result.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(result.Errors);
        }

        var statusTypeResponse = _mapper.Map<StatusTypeResponseDto>(result.Result);

        return Created(statusTypeResponse.Url, statusTypeResponse);
    }

    /// <summary>
    /// Werk een STATUSTYPE in zijn geheel bij.
    /// </summary>
    /// <remarks>
    /// Werk een STATUSTYPE in zijn geheel bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPut(ApiRoutes.StatusTypen.Update, Name = Operations.StatusTypen.Update)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(StatusTypeResponseDto))]
    public async Task<IActionResult> UpdateAsync([FromBody] StatusTypeRequestDto statusTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {@FromBody}, {Uuid}", nameof(UpdateAsync), statusTypeRequest, id);

        StatusType statusType = _mapper.Map<StatusType>(statusTypeRequest);

        var result = await _mediator.Send(
            new UpdateStatusTypeCommand
            {
                StatusType = statusType,
                Id = id,
                ZaakType = statusTypeRequest.ZaakType,
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

        var statusTypeResponse = _mapper.Map<StatusTypeResponseDto>(result.Result);

        return Ok(statusTypeResponse);
    }

    /// <summary>
    /// Werk een STATUSTYPE deels bij.
    /// </summary>
    /// <remarks>
    /// Werk een STATUSTYPE deels bij. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPatch(ApiRoutes.StatusTypen.Update, Name = Operations.StatusTypen.PartialUpdate)]
    [Scope(AuthorizationScopes.Catalogi.Write)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(StatusTypeResponseDto))]
    public async Task<IActionResult> PartialUpdateAsync([FromBody] JObject partialStatusTypeRequest, Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(PartialUpdateAsync), id);

        var resultGet = await _mediator.Send(new GetStatusTypeQuery { Id = id });

        if (resultGet.Status == QueryStatus.NotFound)
        {
            return _errorResponseBuilder.NotFound();
        }

        StatusTypeRequestDto mergedStatusTypeRequest = _requestMerger.MergePartialUpdateToObjectRequest<StatusTypeRequestDto, StatusType>(
            resultGet.Result,
            partialStatusTypeRequest
        );

        if (!_validatorService.IsValid(mergedStatusTypeRequest, out var validationResult))
        {
            return _errorResponseBuilder.BadRequest(validationResult);
        }

        StatusType mergedStatusType = _mapper.Map<StatusType>(mergedStatusTypeRequest);

        var resultUpd = await _mediator.Send(
            new UpdateStatusTypeCommand
            {
                StatusType = mergedStatusType,
                Id = id,
                ZaakType = mergedStatusTypeRequest.ZaakType,
                IsPartialUpdate = true,
            }
        );

        if (resultUpd.Status == CommandStatus.ValidationError)
        {
            return _errorResponseBuilder.BadRequest(resultUpd.Errors);
        }

        var statusTypeResponse = _mapper.Map<StatusTypeResponseDto>(resultUpd.Result);

        return Ok(statusTypeResponse);
    }

    /// <summary>
    /// Verwijder een STATUSTYPE.
    /// </summary>
    /// <remarks>
    /// Verwijder een STATUSTYPE. Dit kan alleen als het bijbehorende ZAAKTYPE een concept betreft.
    /// </remarks>
    /// <response code="204">No content</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    [HttpDelete(ApiRoutes.StatusTypen.Delete, Name = Operations.StatusTypen.Delete)]
    [Scope(AuthorizationScopes.Catalogi.Write, AuthorizationScopes.Catalogi.ForcedDelete)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        _logger.LogDebug("{ControllerMethod} called with {Uuid}", nameof(DeleteAsync), id);

        var result = await _mediator.Send(new DeleteStatusTypeCommand { Id = id });

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
